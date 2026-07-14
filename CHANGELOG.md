# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-07-07

### Changed
- Publisher identity: package id `com.aethernexus.uiwidgets`, author AetherNexus; depends on `com.aethernexus.foundationplatform`
- Added `unityRelease` (`10f1`), Third-Party Notices (UniTask), Asset Store publishing notes
- C# namespaces under `AetherNexus.UIWidgets`
- UniTask: obtained via Foundation Platform embed (no direct UniTask package dependency)
- Relicensed to MIT ahead of publishing
- Decoupled core `UIWidgets.Editor` from `GameEngineCore.Editor`: optional integration assembly gated by `HOMAM_GEC`

### Added
- Standard UPM package files: `package.json`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Documentation~/ARCHITECTURE.md`
- `UIWidgets.GameEngineCoreIntegration.Editor` assembly — optional CentralAuthoring integration

### Moved
- `Runtime/Tests/InputDialogTest.cs` → `Samples~/InputDialogDemo/` — manual demo, not an NUnit test

### Known issues
- `PackageIntegrationManifest.asset` at the package root is a project-internal CentralAuthoring artifact. It deserializes only when GameEngineCore is present; exclude from standalone distribution if shipping without GEC.
- No automated test suite (`Tests/` asmdef) yet
