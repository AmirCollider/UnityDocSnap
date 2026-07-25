#!/usr/bin/env python3
"""
Package validation for Unity DocSnap.

Runs in CI without a Unity licence, and locally with
`python3 .github/scripts/validate_package.py`.

Each check exists because the thing it looks for is cheap to
break and expensive to notice:

  * version drift - package.json and DocSnapConstants.Version
    are both stamped into generated output. They silently
    disagreed once for two releases, which is what
    PackageVersionTests was written for; that test only runs
    inside Unity, and this runs everywhere.
  * missing .meta - a script shipped without one gets a fresh
    random GUID in every user's project, so any reference to
    it rots. Unity regenerates the file locally, so the author
    never sees the problem their users get.
  * site assets - the exporter reads Site~/style.css, app.js,
    fonts.css and logo.svg from disk at export time. If they
    do not ship, every export comes out unstyled and the only
    symptom is a console error nobody reads.
  * required files - LICENSE, README.md, CHANGELOG.md,
    CONTRIBUTING.md, SECURITY.md, .gitignore and
    .gitattributes. The dot-files needed their own check
    precisely because every other check here walks what Unity
    imports, and Unity cannot see a dot-file at all: both were
    announced in a release, neither was ever committed, and
    nothing noticed for two versions.
  * changelog freshness - the NEWEST entry has to be the
    version being shipped. Checking that the version appears
    anywhere in the file stays true forever once it has
    shipped, so a release could go out describing itself with
    the previous version's notes.
"""

import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

failures = []
checks_run = 0


def check(name, condition, detail=""):
    global checks_run
    checks_run += 1
    if condition:
        print("  ok    {}".format(name))
    else:
        print("  FAIL  {} {}".format(name, "- " + detail if detail else ""))
        failures.append(name)


def read(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as handle:
        return handle.read()


# ==========================================
# 1. package.json is valid and complete
# ==========================================
print("package.json")
try:
    package = json.loads(read("package.json"))
    package_ok = True
except Exception as exc:  # noqa: BLE001
    package = {}
    package_ok = False
    check("package.json parses", False, str(exc))

if package_ok:
    check("package.json parses", True)
    for field in ("name", "version", "displayName", "unity", "license"):
        check("has '{}'".format(field), field in package and package[field])
    check(
        "name is reverse-DNS",
        re.match(r"^[a-z0-9]+(\.[a-z0-9-]+)+$", package.get("name", "")) is not None,
        package.get("name", ""),
    )

# ==========================================
# 2. Version is stamped identically everywhere
# ==========================================
print("\nversion consistency")
constants = read("Editor/UnityDocSnap/DocSnapConstants.cs")
match = re.search(r'public const string Version\s*=\s*"([^"]+)"', constants)
check("DocSnapConstants.Version is present", match is not None)

if match and package_ok:
    cs_version = match.group(1)
    pkg_version = package.get("version", "")
    check(
        "package.json {} == DocSnapConstants {}".format(pkg_version, cs_version),
        cs_version == pkg_version,
    )

    changelog = read("CHANGELOG.md")
    check(
        "CHANGELOG documents [{}]".format(pkg_version),
        "[{}]".format(pkg_version) in changelog,
    )

    # The NEWEST entry, not merely "somewhere in the file".
    # Once a version has shipped its heading is in the changelog
    # forever, so the check above passes for every later release
    # whether or not anyone wrote notes for it - which is exactly
    # how a release goes out describing itself with the previous
    # version's changes.
    headings = re.findall(r"^## \[([^\]]+)\]", changelog, re.MULTILINE)
    newest = headings[0] if headings else ""
    check(
        "newest CHANGELOG entry is [{}]".format(pkg_version),
        newest == pkg_version,
        "newest heading is [{}]".format(newest) if newest else "no '## [version]' heading found",
    )

# ==========================================
# 2b. The files a repository is expected to
#     carry actually exist
#
# Dot-files get their own check because every other check in
# this script walks what UNITY imports, and Unity cannot see a
# file whose name starts with a dot. That blind spot is why
# .gitignore and .gitattributes were announced in one release,
# never committed, and went unnoticed until someone looked.
# ==========================================
print("\nrequired files")
for required in (
    "LICENSE",
    "README.md",
    "CHANGELOG.md",
    "CONTRIBUTING.md",
    "SECURITY.md",
    ".gitignore",
    ".gitattributes",
):
    full = os.path.join(ROOT, required)
    exists = os.path.isfile(full)
    check(
        "{} exists".format(required),
        exists and os.path.getsize(full) > 0,
        "missing" if not exists else "empty",
    )

# ==========================================
# 3. Every imported file has a .meta, and no
#    .meta is orphaned
#
# Folders ending in '~' are invisible to Unity by
# design (Docs~, Site~) and must NOT have one.
# ==========================================
print("\n.meta coverage")


def unity_visible_paths():
    """Every file and folder Unity would import, relative to the package root.

    Folders ending in '~' and dot-folders are invisible to Unity, so
    neither they nor anything under them takes part.
    """
    files, folders = set(), set()
    for current, dirs, names in os.walk(ROOT):
        dirs[:] = [d for d in dirs if not d.endswith("~") and not d.startswith(".")]
        rel_dir = os.path.relpath(current, ROOT)
        rel_dir = "" if rel_dir == "." else rel_dir.replace(os.sep, "/")
        for name in dirs:
            folders.add("{}/{}".format(rel_dir, name) if rel_dir else name)
        for name in names:
            if name.startswith("."):
                continue
            files.add("{}/{}".format(rel_dir, name) if rel_dir else name)
    return files, folders


visible, visible_folders = unity_visible_paths()
importable = visible | visible_folders

missing_meta = []
orphan_meta = []

for path in sorted(visible):
    if path.endswith(".meta"):
        # A .meta may describe either a file or a folder.
        if path[: -len(".meta")] not in importable:
            orphan_meta.append(path)
    elif path + ".meta" not in visible:
        missing_meta.append(path)

for path in sorted(visible_folders):
    if path + ".meta" not in visible:
        missing_meta.append(path + "/")

check("no file or folder is missing its .meta", not missing_meta, ", ".join(missing_meta[:8]))
check("no orphaned .meta files", not orphan_meta, ", ".join(orphan_meta[:8]))

# ==========================================
# 4. .meta GUIDs are unique
# ==========================================
print("\n.meta GUIDs")
guids = {}
duplicates = []
for path in sorted(visible):
    if not path.endswith(".meta"):
        continue
    found = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", read(path), re.MULTILINE)
    if not found:
        duplicates.append("{} has no guid".format(path))
        continue
    guid = found.group(1)
    if guid in guids:
        duplicates.append("{} == {}".format(path, guids[guid]))
    guids[guid] = path

check("every .meta carries a unique guid", not duplicates, ", ".join(duplicates[:5]))

# ==========================================
# 5. The site assets the exporter reads at
#    runtime actually ship
# ==========================================
print("\nsite assets (Site~)")
site = "Editor/UnityDocSnap/Site~"
for name, floor in (
    ("style.css", 10000),
    ("app.js", 5000),
    ("fonts.css", 100000),
    ("logo.svg", 200),
):
    full = os.path.join(ROOT, site, name)
    exists = os.path.isfile(full)
    size = os.path.getsize(full) if exists else 0
    check(
        "{}/{} ships ({} bytes)".format(site, name, size),
        exists and size >= floor,
        "missing" if not exists else "only {} bytes, expected >= {}".format(size, floor),
    )

check(
    "Site~ has no .meta files (Unity must ignore it)",
    not [n for n in os.listdir(os.path.join(ROOT, site)) if n.endswith(".meta")]
    if os.path.isdir(os.path.join(ROOT, site))
    else False,
)

# The names the C# asks for must be the names on disk.
site_files_cs = read("Editor/UnityDocSnap/DocSnapSiteFiles.cs")
for name in ("style.css", "app.js", "fonts.css", "logo.svg"):
    check(
        'DocSnapSiteFiles references "{}"'.format(name),
        '"{}"'.format(name) in site_files_cs,
    )

# ==========================================
# 6. No stray absolute paths or leftover scratch
#    files in the shipped package
# ==========================================
print("\nhousekeeping")
strays = [
    p
    for p in visible
    if os.path.basename(p).upper().startswith(("READ-ME-FIRST", "CHANGES_SUMMARY"))
    or p.endswith((".orig", ".rej", ".bak"))
]
check("no scratch files committed", not strays, ", ".join(strays[:5]))

print("\n{} checks, {} failed".format(checks_run, len(failures)))
if failures:
    print("\nFailed: " + ", ".join(failures))
    sys.exit(1)
print("Package looks good.")
