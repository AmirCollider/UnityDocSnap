# Changelog

All notable changes to Unity DocSnap are documented in this file.

## [0.4.0] - 2026-07-23

### Added
- **Simple, AI-friendly Markdown summaries** as a second output alongside the full site. Every export now also writes a short `.md` next to each HTML page (`scenes/<Scene>.md`, `folders/<Key>.md`) plus a project-level `summary.md` index. A Scene summary is a compact hierarchy (one annotated line per GameObject) followed by the serialized configuration of your custom scripts only; an asset-folder summary is a one-line-per-file inventory. Container values (arrays, structs, `[SerializeReference]`) are summarised rather than expanded, so a small Scene lands in a few hundred lines instead of the tens of thousands the exhaustive JSON needs — small enough to paste straight into an AI assistant.
- **Simple / Advanced view toggle** in the generated site (sidebar, next to the language switcher). *Simple* hides the heavy every-field detail — built-in-component field tables, GUIDs, and the collapsed Import Settings / Fields / Shader Properties / Prefab Contents sections — for a fast skim; *Advanced* shows the complete export. The choice is remembered across pages in `localStorage`, exactly like the UI language, and the site opens in Simple by default.

### Changed
- **A successful export now confirms with a single in-Editor popup** (with an *Open Output Folder* button) instead of writing an `export_complete.html` "success" page and opening it in the system browser. Failed exports still use their own native dialog. Any leftover `export_complete.html` from an earlier version is cleaned up on the next export.
- **Cleaner, self-describing output layout.** The folders that were easy to confuse with each other (and with Unity's own `Assets`) are renamed: `assets_ui/` → `theme/` (the site's CSS/JS + thumbnails), `assets/` → `folders/` (the asset-folder pages), and `files/` → `source-files/` (the optional verbatim byte copies). The structured data files are renamed to be self-explanatory: `data/scene_<Name>.json` → `data/scene-<Name>.json` and `data/assets_<Key>.json` → `data/folder-<Key>.json`. Stale `.md` summaries are pruned alongside their pages when a Scene or folder is renamed or removed.
- `package.json` rebuilt with full Unity Package Manager metadata (repository, bugs, license, `type`, `unityRelease`, a fuller keyword set and a proper multi-line description), and `preview-sample.html` rewritten to match the current design and show the new Simple / Advanced toggle.

## [0.3.0] - 2026-07-22

### Fixed
- Asset pages were unreadable when the UI language was switched to Persian. `app.js` sets `dir="rtl"`, but the value spans holding paths, GUIDs, numbers, enums and type names carried no `direction: ltr` / `unicode-bidi: isolate`, so every piece of Latin data was bidi-reordered into fragments. Only `[data-en]` was isolated; the data itself was not. All data-bearing elements are now explicitly LTR-isolated.
- `.ds-asset-grid` used `minmax(300px, 1fr)`. A grid track floor makes the grid wider than its own container whenever the container is narrower than the floor - which is constant inside a nested folder node - so cards overflowed and overlapped. Now `minmax(min(320px, 100%), 1fr)`, with `min-width: 0` on every grid child.
- `.ds-field-grid` had `96px` + `64px` column floors that overflowed narrow cards; `.ds-asset-card`'s `overflow: hidden` then silently clipped the data, which is why Import Settings looked present but unreadable. Floors removed and a container query stacks the grid when a card is genuinely narrow.
- `.ds-tree ul` added 33px of indentation per nesting level, compounding into the two failures above at depth. Indentation now tapers with depth.
- `.ds-kv-line` was a flex row with a non-shrinkable key, so one long path forced the whole card wider than its grid track. Rebuilt as a two-column grid.
- **"Export Full Project With Files" copied every asset's bytes into `files/` and then never referenced them from any page.** No `physicalFile` key was written to the JSON entry and no renderer emitted `<img>`, `<audio>`, `<video>` or a download link, so images and audio never appeared even after a with-files export. Asset cards now play audio and video inline, show the real image file, and expose Open / Download for every copied asset.
- `ThumbnailGenerator.BlitResize` created its RenderTexture with default read/write. In a Linear color-space project this produced gamma-incorrect (washed out or near-black) thumbnails. It now requests `RenderTextureReadWrite.sRGB` when the project is Linear.
- `AssetDatabase.FindAssets(string.Empty, ...)` is undocumented and returns nothing on some Unity versions, producing a silently empty Assets page. `CollectFilePaths` now walks the filesystem and validates each candidate against the AssetDatabase.
- `SanitizeAnchor` collapsed every non-alphanumeric character to `-`, so `UI_Menu`, `UI/Menu` and `UI Menu` all produced the same DOM id. A stable FNV-1a suffix now keeps them distinct.
- `BuildGuidLookup` resolved a GUID indexed under several folders by last-writer-wins, so cross-links pointed at a different page between runs. Resolution is now deterministic, preferring the most specific page.
- `AssetPageRenderer`'s path map was case-sensitive while the folder tree normalised separators, so case drift dropped files out of the rendered tree. Now `OrdinalIgnoreCase`.
- `.ds-matrix-row-head` used the physical `left` property and broke under RTL. Now `inset-inline-start`.

### Changed
- Thumbnails are written as real `assets_ui/thumbs/<guid>.png` files instead of base64 data URIs inlined into both the HTML and the JSON. Previews now lazy-load and cache; a large project's Asset page is no longer a single multi-hundred-megabyte document. Callers that pass no output root still receive the base64 form.
- Import Settings, Fields, Shader Properties and Prefab Contents render inside a collapsed `<details>`. A closed `<details>` is parsed but never laid out, which is the difference between a folder page opening and a folder page hanging the browser.
- Each folder node renders at most `MaxAssetsRenderedPerFolderNode` (300) asset cards, with a count of the remainder. The complete list always remains in `data/assets_*.json`.
- `.tga`, `.psd`, `.exr`, `.tif`, `.webp` and `.bmp` textures - plus Materials, Prefabs, Models and Fonts - now get Unity's real rendered preview via a short polling wait, instead of falling through to a 16x16 type icon. Icons, when still used, are tagged and sized as small badges rather than stretched across the preview box.
- Every generated URL is percent-encoded per path segment. Asset paths containing spaces or `#` previously produced broken links.
- Full-project exports drive an Editor progress bar. They previously ran with no feedback at all and looked frozen.
- Every export prunes scene and asset pages whose source no longer appears in the manifest, instead of leaving orphaned documents behind forever.

## [0.2.3] - 2026-07-08

### Fixed
- Nested/jagged numeric arrays (vertex lists, matrices, per-bone weight tables, …) could still split a value mid-character (e.g. `0.399261` into `0.39` / `9261`) despite the 0.2.1/0.2.2 fixes, because `.ds-nested-table`'s nested `<table>` and `.ds-array-item`'s `overflow-wrap: anywhere` could still be squeezed into a narrow column once enough levels of nesting compounded a fixed percentage width. The entire field/array rendering pipeline (shared by Asset pages and Scene pages) has been rebuilt instead of patched again: `<table>`-based field rows are replaced by a CSS Grid row layout (`.ds-field-grid`, rows using `display: contents`) that builds its own column tracks from its own available width at every nesting level, so a fixed percentage can no longer compound down through nested structs/components/arrays.

### Changed
- Compact array elements (numbers, bools, enums, vectors, references, …) now render as fixed-width grid cells (`.ds-array-grid` / `.ds-array-cell`) using `white-space: nowrap` + `text-overflow: ellipsis` instead of the old `.ds-array-wrap` / `.ds-array-item` flex-wrap chips - a value either fits or truncates with an ellipsis, and the untruncated value is always available as a hover tooltip.
- `RenderShaderProps` (Material shader property tables) now shares the same field-grid renderer as every other field table instead of its own separate `<table class="ds-field-table">` markup.
- Object reference chips (`.ds-ref-chip`) now truncate with an ellipsis and carry a `title` tooltip with the full target name, instead of wrapping (and potentially breaking mid-character) a long name.

### Added
- Jagged/2D scalar arrays (an array whose own elements are arrays) are now detected and rendered as one real, sticky-header, horizontally+vertically scrollable spreadsheet-style table (`.ds-matrix-scroll` / `.ds-matrix-table`) instead of nesting an array block inside another array block - this specific shape of data was the actual root cause behind the character-splitting bug recurring after every previous patch.

## [0.2.2] - 2026-07-08

### Fixed
- Asset folder pages: array/list fields with many elements (in particular jagged/nested arrays - an array whose own elements are arrays) no longer render as an unbounded vertical stack that could reach hundreds of thousands of pixels tall and force text into a column only a few characters wide (`.ds-field-value`'s `overflow-wrap: anywhere` breaking numbers like `0.399261` into `0.39` / `9261` once the column got squeezed that narrow). Simple scalar array elements (numbers, bools, vectors, enums, references, …) now render as small wrapping chips inside a fixed-height (240px), scrollable box (`.ds-array-wrap` / `.ds-array-item`); only genuinely complex elements (nested structs, `[SerializeReference]` values, nested arrays) still render as their own full-width line.
- `UniversalReflector.ReadArray` now tracks array-nesting depth and applies a much smaller `MaxNestedArrayElementsRendered` cap to any array reached from inside another array, instead of reusing the same top-level `MaxArrayElementsRendered` cap at every nesting level - this compounding (up to 200 × 200 elements previously) was the root cause of both the export bloat and the multi-minute-scroll pages reported for Asset folders with jagged numeric array fields.
- `.ds-nested-table` no longer inherits the outer `.ds-field-table`'s fixed 32% / 22% / 46% column widths verbatim at every nesting level, which compounded into unreadably narrow columns a few levels deep. Nested field tables now use `table-layout: auto` with a `min-width`, scrolling horizontally inside their own card (`overflow-x: auto`) instead of being squeezed.

### Changed
- `DocSnapConstants.MaxArrayElementsRendered` lowered from `200` to `50`; new `DocSnapConstants.MaxNestedArrayElementsRendered` (`10`) applies specifically to nested/jagged arrays, so a single field's export payload is substantially smaller by default.

## [0.2.1] - 2026-07-08

### Fixed
- Asset folder pages: `Import Settings`/`Fields`/shader-property tables no longer overflow their card and get silently clipped by `.ds-asset-card`'s `overflow: hidden`. `.ds-field-table` now uses `table-layout: fixed` with fixed Field/Type/Value column-width percentages instead of sizing columns from content, and cells wrap (`overflow-wrap: anywhere`) instead of forcing the table wider than its column - this was the root cause of Import Settings data rendering unreadable, jumbled, and mis-sized for assets with many fields (Texture importers in particular).
- `.ds-asset-grid` cards widened from a 240px to a 300px minimum column, and asset/GameObject card title headers (`h3`) no longer force the card wider for long, unbroken filenames - both now shrink and wrap correctly inside the grid.
- Image assets (`TextureImporter` assets) no longer fall back to the bare placeholder glyph when `Generate Image Thumbnails` is off: a generic type icon is now always attempted for images too, matching the existing behavior already in place for every other asset type.
- `DocSnapSettings.GenerateThumbnails` now defaults to **on** (previously off), so a first export already shows real image previews on Asset pages instead of a blank icon. The project setting still lets anyone opt back into DocSnap's stricter "pixels never leave your project" mode.

## [0.2.0] - 2026-07-07

### Fixed
- `SceneHierarchyExporter.ExportScene` no longer spams the console with Unity's "More than one global light on layer … for light blend style index 0" warning on every `Export Full Project` run. The additive scene load/unload DocSnap performs internally now runs with `Debug.unityLogger.logEnabled` temporarily off, since the warning is a transient, tool-caused side effect (two Scenes' Global Light 2Ds briefly coexisting) rather than a real project issue.
- `UniversalReflector` no longer logs "type is not a supported int value" for property types this reflector does not explicitly recognise (e.g. `RenderingLayerMask`). The first attempt at this fix still guessed with `longValue`/`stringValue`/`boolValue` past the first special case, and a mismatched accessor makes Unity log that error directly from native code rather than throwing a catchable exception - no try/catch can stop that. `boxedValue` is used instead, since it is built specifically to read a property's value without needing to know its concrete type in advance.- Asset folder pages: the card grid no longer uses a fixed `column-width`, which broke down (overlapping text, mis-sized cards) whenever a card's content - long paths, GUIDs, or wide import-setting tables - needed more room than the fixed column allowed. It is now a responsive CSS Grid that sizes columns to the available width.

### Added
- Asset folder pages now render a real, collapsible directory tree (mirroring the existing Scene Hierarchy tree UI) instead of one flat file grid. Every exported folder's subfolders can be expanded or collapsed individually, each showing only the files that live directly inside it.
- `Unity DocSnap > Export Full Project With Files`: identical to `Export Full Project`, but additionally copies every referenced asset's actual file bytes (plus its `.meta` file, when present) into an output-side `files/` folder that mirrors each asset's original `Assets/…` relative path. The original `Export Full Project` and `Export Asset Info` actions remain metadata-only, matching DocSnap's existing "asset info, never asset files" default.
- Every successful export now finishes with a small, on-brand confirmation page (green accent, message repeated in English / Japanese / Persian) opened in the browser, instead of a plain native OK dialog. Failed exports still use a native `EditorUtility.DisplayDialog`, so a real problem still looks like one.

## [0.1.0] - 2026-07-07

### Fixed
- Added the `.meta` file for every folder and file in the package. Without these, Unity's Package Manager (git URL installs in particular) treats the package cache as immutable and silently ignores every asset, logging "has no meta file, but it's in an immutable folder" for each one.

### Added
- `Unity DocSnap` top-level Editor menu: Export Scene (per-scene dropdown), Export Asset Info (entire Assets folder or a chosen folder), Export Full Project, Open Output Folder, About.
- Right-click actions in the Project window for Scenes, folders, and individual assets.
- Full Scene Hierarchy export: every GameObject, every Component, every serialized Inspector field, via a generic `SerializedObject` reflector.
- Object-reference fields become clickable cross-links between GameObjects, Prefabs, Materials, and other assets.
- Asset folder export: import settings for every file (recursively), Material shader properties, Prefab contents, lightweight script metadata, and image previews - metadata only, never the source files themselves.
- Self-contained offline site (`index.html` + one page per Scene/folder) with a live sidebar, English / Japanese / Persian UI language switching, and a kawaii visual theme.
- Parallel `data/*.json` export for AI tools and other scripts to consume directly.
- Zero third-party dependencies.
