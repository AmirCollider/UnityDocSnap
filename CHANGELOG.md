# Changelog

All notable changes to Unity DocSnap are documented in this file.

## [0.2.0] - 2026-07-07

### Fixed
- `SceneHierarchyExporter.ExportScene` no longer spams the console with Unity's "More than one global light on layer … for light blend style index 0" warning on every `Export Full Project` run. The additive scene load/unload DocSnap performs internally now runs with `Debug.unityLogger.logEnabled` temporarily off, since the warning is a transient, tool-caused side effect (two Scenes' Global Light 2Ds briefly coexisting) rather than a real project issue.
- `UniversalReflector` no longer logs "type is not a supported int value" for `RenderingLayerMask` fields. These are now detected by property-type name (no hard compile-time dependency on the enum member) and read via `uintValue` instead of falling through to a `longValue` read that Unity itself cannot service for this type.
- Asset folder pages: the card grid no longer uses a fixed `column-width`, which broke down (overlapping text, mis-sized cards) whenever a card's content - long paths, GUIDs, or wide import-setting tables - needed more room than the fixed column allowed. It is now a responsive CSS Grid that sizes columns to the available width.

### Added
- Asset folder pages now render a real, collapsible directory tree (mirroring the existing Scene Hierarchy tree UI) instead of one flat file grid. Every exported folder's subfolders can be expanded or collapsed individually, each showing only the files that live directly inside it.
- `Unity DocSnap > Export Full Project With Files`: identical to `Export Full Project`, but additionally copies every referenced asset's actual file bytes (plus its `.meta` file, when present) into an output-side `files/` folder that mirrors each asset's original `Assets/…` relative path. The original `Export Full Project` and `Export Asset Info` actions remain metadata-only, matching DocSnap's existing "asset info, never asset files" default.

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
