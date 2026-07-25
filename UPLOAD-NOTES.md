# Upload notes — Unity DocSnap 0.11.0

39 files. Every path in this archive is relative to the repository
root, so extracting it over a clean checkout puts each file exactly
where it belongs and overwrites nothing it should not.

**Delete this file before committing** — it is a note to you, not part
of the package. `validate_package.py` does not check for it, but it
does not belong in a release either.

---

## ⚠️ Two files GitHub's web uploader will silently drop

```
.gitignore
.gitattributes
```

The drag-and-drop uploader on github.com **ignores dot-files without
saying so.** This is almost certainly why both files were announced in
the 0.9.1 release notes, announced again in 0.10.0 along with a CI
check for them, and are still not in the repository — which means
`validate_package.py` has been failing on every push since 0.10.0.

If you upload through the browser, add these two by another route:

- **Web, one at a time** — `Add file ▸ Create new file`, type
  `.gitignore` as the name, paste the contents. Same for
  `.gitattributes`.
- **Git** — `git add -f .gitignore .gitattributes` (the `-f` is not
  needed here, but is harmless and covers the case where a global
  ignore rule of your own would otherwise skip them).

After pushing, the `Validate package` CI job should report
**35 checks, 0 failed**. If it still reports 2 failures naming these
files, they did not land.

---

## What is in the archive

| Area | Files |
|---|---|
| New source | `DocSnapOutputPath.cs`, `DocSnapOutputPathMessages.cs`, `DocSnapLanguages.cs`, `DocSnapText.cs`, `DocSnapTranslations.cs` (+ their `.meta`) |
| New tests | `DocSnapOutputPathTests.cs`, `DocSnapLanguageRegistryTests.cs`, `JsonValueStreamingTests.cs`, `DocSnapLegacyCleanupTests.cs` (+ their `.meta`) |
| Modified source | 15 files across `Editor/` |
| Site | `Site~/app.js` |
| Packaging | `package.json`, `CHANGELOG.md`, `.github/scripts/validate_package.py` |
| Repo hygiene | `.gitignore`, `.gitattributes` |

Every new `.cs` file ships with a `.meta` carrying a freshly generated
GUID, checked against all 85 GUIDs already in the package. Unity will
not reassign them, so references stay stable.

`Site~/app.js` has no `.meta` and must not get one — the `~` suffix is
what keeps that folder invisible to Unity, and the validator fails the
build if a `.meta` appears there.

---

## After extracting

Run the validator, which needs no Unity licence:

```bash
python3 .github/scripts/validate_package.py
```

Expected: `35 checks, 0 failed`.

---

## Not done, and why

**The EditMode tests still do not run in CI.** They need `UNITY_LICENSE`,
`UNITY_EMAIL` and `UNITY_PASSWORD` in the repository secrets — that is
your account, not something a code change can supply. Until they are
set, the job reports "skipped" rather than failing, which is the right
behaviour for forks but does mean the 312 tests only ever run on your
machine. See the comment block in `.github/workflows/ci.yml`, and
https://game.ci/docs/github/activation for producing the `.ulf` file.

**Tagging and the first release** — yours, as you said.
