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
# 6. Invariants that no licence can check for us
#
# The EditMode tests need a Unity licence, which a
# fork cannot have and which this repository may not
# have configured - so the job that runs them is
# skipped far more often than it runs. That makes it
# the wrong place for rules that are cheap to state
# textually, and these three are exactly that shape:
# each one is a rule the codebase deliberately holds,
# each was violated at least once before it was a
# rule, and each re-breaks by someone writing one
# ordinary-looking line in a file they were already
# editing.
# ==========================================
print("\ncodebase invariants")

editor_sources = {}
for current, dirs, names in os.walk(os.path.join(ROOT, "Editor")):
    dirs[:] = [d for d in dirs if not d.startswith(".")]
    for name in names:
        if name.endswith(".cs"):
            full = os.path.join(current, name)
            editor_sources[os.path.relpath(full, ROOT).replace(os.sep, "/")] = read(full)


def code_lines(source):
    """Lines with // comments and string literals removed.

    Crude on purpose: it only has to stop a comment or a
    Japanese string constant from reading as code. A rule that
    needs more than this does not belong in a text scan.
    """
    out = []
    for line in source.split("\n"):
        stripped = line.strip()
        if stripped.startswith("//") or stripped.startswith("*") or stripped.startswith("/*"):
            continue
        out.append(re.sub(r'"(?:[^"\\]|\\.)*"', '""', line.split("//")[0]))
    return out


# --- The language registry is the only place that
#     knows which languages exist. A comparison
#     against a bare "ja" / "fa" anywhere else is how
#     the registry silently stops being the source of
#     truth: the language renders, and then lays
#     itself out in the wrong direction.
LANGUAGE_REGISTRY_FILES = {
    "Editor/UnityDocSnap/DocSnapLanguages.cs",   # defines them
    "Editor/UnityDocSnap/DocSnapText.cs",        # resolves them
}
hardcoded_language = []
for path, source in sorted(editor_sources.items()):
    if path in LANGUAGE_REGISTRY_FILES:
        continue
    for number, line in enumerate(code_lines(source), start=1):
        if re.search(r'==\s*"(ja|fa)"|"(ja|fa)"\s*==', line):
            hardcoded_language.append("{}:{}".format(path, number))

check(
    "no language code is compared outside the registry",
    not hardcoded_language,
    ", ".join(hardcoded_language[:5]),
)

# --- Recursive directory deletion. There are exactly
#     three of these, all in code that has proved it
#     owns the folder first. A fourth is not
#     automatically wrong, but it is never something
#     to add without noticing.
recursive_deletes = []
for path, source in sorted(editor_sources.items()):
    for number, line in enumerate(code_lines(source), start=1):
        if re.search(r"Directory\.Delete\([^)]*,\s*true\s*\)", line):
            recursive_deletes.append("{}:{}".format(path, number))

check(
    "recursive deletes stay accounted for (found {})".format(len(recursive_deletes)),
    len(recursive_deletes) <= 3,
    ", ".join(recursive_deletes),
)

# --- The output folder must never be allowed inside
#     Assets: Unity imports everything there, and the
#     next export documents the previous one. The rule
#     lives in DocSnapOutputPath and the gate is in the
#     export service; if either disappears, nothing at
#     runtime says so until a user's project has
#     thousands of generated files in it.
output_path_cs = editor_sources.get("Editor/UnityDocSnap/DocSnapOutputPath.cs", "")
check(
    "DocSnapOutputPath rejects an output folder inside Assets",
    "InsideAssets" in output_path_cs and '"Assets"' in output_path_cs,
)
check(
    "the export gate validates the output folder",
    "ValidateOutputRoot()" in editor_sources.get("Editor/UnityDocSnap/Export/DocSnapExportService.cs", ""),
)

# --- The edition gate.
#
#     Every one of these is a line that could be deleted
#     during an unrelated refactor without any test noticing
#     inside Unity - the EditMode suite needs a licence CI
#     usually does not have, so it is skipped far more often
#     than it runs. Each check below is a paid feature quietly
#     becoming free, which is not a failure anybody reports.
export_service = editor_sources.get("Editor/UnityDocSnap/Export/DocSnapExportService.cs", "")
docsnap_api = editor_sources.get("Editor/UnityDocSnap/DocSnapAPI.cs", "")
page_builder = editor_sources.get("Editor/UnityDocSnap/Html/HtmlPageBuilder.cs", "")
edition_matrix = editor_sources.get("Editor/UnityDocSnap/Licensing/DocSnapEdition.cs", "")
token_source = editor_sources.get("Editor/UnityDocSnap/Licensing/DocSnapLicenseToken.cs", "")

check(
    "the summary/ writers check the edition",
    export_service.count("DocSnapEditionGate.WritesAiSummaries") >= 3,
    "expected the gate in WriteSceneSummaries, WriteFolderSummaries and WriteAiBundle",
)

check(
    "export options are clamped to the licensed edition",
    "DocSnapEditionGate.ClampOptions" in export_service,
)

check(
    "the free version-shelf cap is applied",
    "DocSnapEditionGate.ResolveVersionCap" in export_service,
)

check(
    "scripted exports check the Automation feature",
    "DocSnapFeature.Automation" in docsnap_api,
)

check(
    "a custom logo stays gated",
    "DocSnapFeature.CustomLogo" in page_builder,
)

# --- The free-edition footer line and the custom logo are two
#     different rules, and collapsing them into one is the
#     mistake worth catching. A Plus customer has paid; a page
#     telling them on every export that they are running the
#     free edition would simply be false.
check(
    "the free-edition badge is driven by the tier, not by a feature flag",
    "ShowsFreeEditionBadge" in page_builder,
)

# --- The tier of each paid feature. These two lines ARE the
#     price list, and a feature sliding between them moves real
#     money without any test inside Unity noticing - the EditMode
#     suite needs a licence CI usually does not have.
def priced_at(feature, tier):
    return re.search(
        r"\{\s*DocSnapFeature\." + feature + r"\s*,\s*DocSnapEdition\." + tier + r"\s*\}",
        edition_matrix,
    ) is not None


check("AI summaries are priced at Plus", priced_at("AiSummaries", "Plus"))
check("the Changes page is priced at Plus", priced_at("ChangesPage", "Plus"))

for feature in ("UnlimitedVersions", "IncrementalUpdate", "IncludeFiles",
                "ProjectBackup", "Automation", "CustomLogo"):
    check("{} is priced at Pro".format(feature), priced_at(feature, "Pro"))

# --- Allows() is a >= comparison over the enum's numeric order,
#     so the order is load-bearing rather than cosmetic:
#     renumbering it hands Plus everything Pro has.
# --- Plus must keep strictly more snapshots than Free. A paid
#     tier that behaves identically to the free one in a visible
#     respect is a tier the customer learns not to trust.
free_shelf = re.search(r"FreeVersionFolders\s*=\s*(\d+)", edition_matrix)
plus_shelf = re.search(r"PlusVersionFolders\s*=\s*(\d+)", edition_matrix)
check(
    "Plus keeps more version snapshots than Free",
    free_shelf is not None
    and plus_shelf is not None
    and int(plus_shelf.group(1)) > int(free_shelf.group(1)),
    "free={} plus={}".format(
        free_shelf.group(1) if free_shelf else "?",
        plus_shelf.group(1) if plus_shelf else "?",
    ),
)

check(
    "the tier ladder is ordered Free < Plus < Pro",
    re.search(r"Free\s*=\s*0", edition_matrix) is not None
    and re.search(r"Plus\s*=\s*1", edition_matrix) is not None
    and re.search(r"Pro\s*=\s*2", edition_matrix) is not None,
)

# --- The licence server's public key.
#
#     A 2048-bit RSA modulus is 256 bytes, which is 342
#     characters of unpadded base64url. A placeholder, a
#     truncated paste or a key swapped for a different one all
#     produce a package whose every activation succeeds on the
#     server and is then rejected by the Editor that asked for
#     it - the single worst failure this system has, because it
#     only appears after somebody has paid.
modulus_match = re.search(
    r"PublicModulus\s*=\s*((?:\s*\"[^\"]*\"\s*\+?)+)\s*;", token_source
)
modulus = "".join(re.findall(r'"([^"]*)"', modulus_match.group(1))) if modulus_match else ""

check(
    "the embedded licence public key is a full RSA-2048 modulus",
    len(modulus) == 342 and re.fullmatch(r"[A-Za-z0-9_-]+", modulus) is not None,
    "got {} base64url chars, expected 342".format(len(modulus)),
)

check(
    "the licence public key is not a placeholder",
    "REPLACE" not in modulus and "XXXX" not in modulus,
)

# ==========================================
# 7. No stray absolute paths or leftover scratch
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
