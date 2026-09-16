#!/usr/bin/env python3
# ==========================================
# validate_release.py
#
# Validates the RELEASED package - the tree a user actually
# installs - from the public repository, with no Unity licence
# and no secrets, so a fork and a pull request get the same
# answer the maintainer does.
#
# It is deliberately NOT the private repository's
# validate_package.py. That script walks the .cs sources to check
# things like edition gating and the language registry, and the
# entire point of a release is that there are no .cs sources here.
# Pointing it at this tree would report every check as vacuously
# passing, which is worse than not running it.
#
# So this checks what can be checked from the shipped bytes, and
# every one of these has a failure mode that only shows up in
# somebody else's Unity:
#
#   the assembly is present and carries the version package.json
#   claims        - built before the bump is the classic one, and
#                   it ships a tool that reports the wrong version
#                   in every export it writes.
#   Site~ is complete and carries no .meta
#                 - the exporter reads those four files off disk
#                   at export time. Missing, every export comes
#                   out unstyled and the only symptom is a console
#                   error nobody reads. A .meta in there means
#                   Unity stopped ignoring the folder.
#   every shipped file has its .meta
#                 - without one Unity invents a GUID per install,
#                   so every reference to the file rots and the
#                   author never sees it.
#   no source or symbols leaked
#                 - a .cs, .pdb or .mdb here is the packaging
#                   script having grown a hole.
#   no private key material
#                 - the licence signing key lives on one machine
#                   and must never reach a public byte.
#
# Exit code is 1 on any failure, so CI goes red.
# ==========================================

import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSEMBLY = "AmirCollider.UnityDocSnap.Editor.dll"
SITE_FILES = ["style.css", "app.js", "fonts.css", "logo.svg"]

failures = []


def check(label, condition, detail=""):
    if condition:
        print("  ok    " + label)
    else:
        failures.append(label)
        print("  FAIL  " + label + (" - " + detail if detail else ""))


def read_bytes(path):
    with open(path, "rb") as handle:
        return handle.read()


print("package.json")
package_path = os.path.join(ROOT, "package.json")
package = {}
if os.path.isfile(package_path):
    with open(package_path, encoding="utf-8") as handle:
        package = json.load(handle)
check("package.json exists and parses", bool(package))

version = package.get("version", "")
check("package.json declares a version", bool(version), "no version field")
check('package.json does not claim MIT', package.get("license", "") != "MIT",
      "the released package is not MIT")

print("\nrequired files")
for required in ("LICENSE", "README.md", "CHANGELOG.md", "SECURITY.md",
                 ".gitignore", ".gitattributes"):
    check(required + " exists", os.path.isfile(os.path.join(ROOT, required)), "missing")

print("\nthe assembly")
dll_path = os.path.join(ROOT, "Editor", "UnityDocSnap", ASSEMBLY)
have_dll = os.path.isfile(dll_path)
check("the Editor assembly ships", have_dll, dll_path + " is missing")

if have_dll and version:
    blob = read_bytes(dll_path)
    # The constant survives obfuscation as a literal, and reading
    # the bytes needs no .NET runtime to be installed.
    carries = version.encode("utf-16-le") in blob or version.encode("utf-8") in blob
    check("the assembly carries the version string " + version, carries,
          "it was probably built before package.json was bumped")
    check("no private key material in the assembly",
          not re.search(rb"DOCSNAP_LICENSE_PRIVATE_KEY|BEGIN RSA PRIVATE KEY|BEGIN PRIVATE KEY", blob))
    check("the assembly has its .meta", os.path.isfile(dll_path + ".meta"),
          "every install would invent its own GUID for it")

print("\nsite assets")
site = os.path.join(ROOT, "Editor", "UnityDocSnap", "Site~")
check("Site~ ships", os.path.isdir(site), "exports would be unstyled")
if os.path.isdir(site):
    for name in SITE_FILES:
        path = os.path.join(site, name)
        check("Site~/" + name + " is present and not empty",
              os.path.isfile(path) and os.path.getsize(path) > 0)
    stray = []
    for folder, _dirs, files in os.walk(site):
        stray.extend(name for name in files if name.endswith(".meta"))
    check("Site~ carries no .meta files", not stray,
          "Unity must ignore that folder entirely: " + ", ".join(stray[:5]))

print("\nno source reached the package")
leaked = []
for folder, dirs, files in os.walk(ROOT):
    dirs[:] = [d for d in dirs if d != ".git"]
    for name in files:
        if name.endswith((".cs", ".cs.meta", ".asmdef", ".asmdef.meta", ".pdb", ".mdb")):
            leaked.append(os.path.relpath(os.path.join(folder, name), ROOT))
check("no .cs, .asmdef, .pdb or .mdb in the package", not leaked,
      ", ".join(sorted(leaked)[:10]))

print("\nmeta files")
missing = []
for folder, dirs, files in os.walk(ROOT):
    parts = folder.split(os.sep)
    if any(p.startswith(".") for p in parts) or any(p.endswith("~") for p in parts):
        continue
    dirs[:] = [d for d in dirs if not d.endswith("~") and not d.startswith(".")]
    for name in files:
        if name.endswith(".meta") or name.startswith("."):
            continue
        if not os.path.isfile(os.path.join(folder, name + ".meta")):
            missing.append(os.path.relpath(os.path.join(folder, name), ROOT))
    for name in dirs:
        if not os.path.isfile(os.path.join(folder, name + ".meta")):
            missing.append(os.path.relpath(os.path.join(folder, name), ROOT) + "/")
check("every shipped file and folder has its .meta", not missing,
      ", ".join(sorted(missing)[:10]))

print("")
total = "checks"
if failures:
    print("{} failed: {}".format(len(failures), ", ".join(failures)))
    sys.exit(1)
print("The released package looks good.")
