# Changelog

All notable changes to Unity DocSnap are documented in this file.

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
