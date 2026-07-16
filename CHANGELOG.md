# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed
- **LayoutX**: Compact wrap / preferred sizes (CSF-safe), SizeControl.None vs Preferred measure, layout rebuild on both axes

### Changed
- **LayoutX**: `vertical` bool → `Main Axis` enum (`Horizontal` / `Vertical`); adds reverse arrangement + force expand main/cross
- Hierarchy create menus flattened under `GameObject/UI (Canvas)/` (unique widgets only; no parallel `UIWidgets` create root)
- Dropped curated stock clones (Image/Text/Toggle/etc.) from catalog; Window/Scene Overlay **Default UI** section creates Unity/TMP stock controls instead
- Canvas/EventSystem helpers always use stock Unity objects
- Hierarchy menu: removed Layout X (use palette Behaviours / Add Component), ButtonTMP, and Setup Default State (palette Behaviours covers UI Default State)
- **ToastUI**: FIFO queue, layout-only positioning (no size-destroying anchors), luminance text contrast, fail-fast color/ref validation, click-to-dismiss raycasts + ButtonX wiring; builder adds `WithSeverity`, `Replace`, `WithIcon`, `DismissAll`

## [1.0.0] - 2026-07-07

### Added
- Free Asset Store UPM package: `com.aethernexus.uiwidgets` (author AetherNexus)
- UGUI widgets: panels/modals, ButtonX, ScrollList, tabs, sliders, cards, toasts, tooltips, procedural graphics, UI effects, LayoutX
- Editor: UI Widgets window, GameObject/UI (Canvas) create menus, Scene Picker, Text→TMP migration, ButtonX upgrade
- Sample: **UI Widgets Demo** (`Samples~/UIWidgets Demo`) — Dialog / InputDialog / Fader / LineMessage / LoadingPanel / WaitPanel / ModalService harnesses + scene
- Docs: README, Documentation~/index, ARCHITECTURE, SAMPLES, LICENSE (MIT), Third-Party Notices

### Changed
- Depends on Foundation Platform (`com.aethernexus.foundationplatform`) for UniTask embed and shared runtime — no direct UniTask package dependency
- Public namespaces: `AetherNexus.UIWidgets` / `AetherNexus.UIWidgets.Editor`

### Notes
- Requires Unity **6000.3.10f1+**. Fast Enter Play Mode is **not** supported — keep Domain Reload enabled.
- `PackageIntegrationManifest.asset` is optional metadata for GameEngineCore Central Authoring; unused when that product is not installed.
