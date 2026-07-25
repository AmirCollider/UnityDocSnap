# Contributing to Unity DocSnap

Thanks for looking. Issues and pull requests are both welcome, including small ones.

## Where things live

| Path | What it is |
| --- | --- |
| `Editor/UnityDocSnap/` | The tool. Everything is `internal` except `DocSnapAPI`. |
| `Editor/UnityDocSnap/Site~/` | The generated site's real `style.css`, `app.js`, `fonts.css` and `logo.svg`. Edit them directly — they are read at export time and written into each version folder. The trailing `~` keeps Unity from importing them, which is why they need no `.meta`. |
| `Tests/Editor/` | EditMode tests (NUnit), reaching the `internal` types through `InternalsVisibleTo`. |
| `.github/scripts/validate_package.py` | Everything CI checks without a Unity licence. |

## Before opening a pull request

```bash
python3 .github/scripts/validate_package.py
```

That covers version agreement across `package.json`, `DocSnapConstants.Version` and `CHANGELOG.md`; `.meta` coverage and GUID uniqueness; the site assets actually shipping; and the repository hygiene files existing.

Then run the EditMode tests from **Window → General → Test Runner** in any project that has the package installed. CI runs both on every push, and the Unity tests against 2021.3 (the declared floor) and Unity 6.

## Things worth knowing

**Every new script needs a `.meta` file.** A script shipped without one gets a fresh random GUID in every user's project, so every reference to it rots — and because Unity regenerates the file locally, the author never sees the problem their users get. The validation script fails on this.

**`internal` is the default.** `DocSnapAPI` is the only public type, deliberately: a public type is a promise about a shape that then cannot change freely. If something needs to be reachable from outside, it belongs on `DocSnapAPI`, not exposed where it sits.

**Comments explain *why*.** The house style is that a comment says what a future reader could not work out from the code — the bug this shape prevents, the trade-off that was taken, the thing that was tried and did not work. If a comment restates the line below it, it is not carrying its weight.

**Anything that deletes a file needs a test.** `PruneStaleOutput` is the only code in the tool that deletes anything, and the version folder it deletes from is the artefact the user keeps. Everything else the tool gets wrong can be fixed by exporting again; that cannot.

**Three languages.** User-facing strings in the Editor and in the generated site are English, Japanese and Persian. A new string needs all three; `HtmlPageBuilder.I18n` is how the site carries them.

## Releases

`package.json`, `DocSnapConstants.Version` and the newest `CHANGELOG.md` heading all carry the version and must agree — CI fails if they do not, including if the newest CHANGELOG entry is not the version being shipped.

```bash
git tag v0.10.1 && git push origin v0.10.1
```

The release workflow builds the GitHub Release from the matching CHANGELOG section. Tagging is what lets a user pin a version in the Package Manager (`…UnityDocSnap.git#v0.10.1`) instead of always getting whatever the default branch happens to be that day.
