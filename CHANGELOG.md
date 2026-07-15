# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-07-07

### Added
- Free Asset Store UPM package: `com.aethernexus.uiwidgets` (author AetherNexus)
- UGUI widgets: panels/modals, ButtonX, ScrollableList, tabs, sliders, cards, toasts, tooltips, procedural graphics, UI effects, LayoutX
- Editor: UI Widgets window, GameObject/UIWidgets create menus, Scene Picker, Text→TMP migration, ButtonX upgrade
- Sample: **UI Widgets Demo** (`Samples~/UIWidgets Demo`) — Dialog / InputDialog / Fader / LineMessage / LoadingPanel / WaitPanel / ModalService harnesses + scene
- Docs: README, Documentation~/index, ARCHITECTURE, SAMPLES, LICENSE (MIT), Third-Party Notices

### Changed
- Depends on Foundation Platform (`com.aethernexus.foundationplatform`) for UniTask embed and shared runtime — no direct UniTask package dependency
- Public namespaces: `AetherNexus.UIWidgets` / `AetherNexus.UIWidgets.Editor`
- Optional GameEngineCore Central Authoring integration is gated by scripting define `HOMAM_GEC` (inactive without GameEngineCore)

### Notes
- Requires Unity **6000.3.10f1+**. Fast Enter Play Mode is **not** supported — keep Domain Reload enabled.
- `PackageIntegrationManifest.asset` is optional metadata for GameEngineCore Central Authoring; unused when that product is not installed.
