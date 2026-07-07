# Changelog

All notable changes to this package are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-07-07

### Added
- Standard UPM package files: `package.json`, `README.md`, `CHANGELOG.md`, `LICENSE.md`, `Documentation~/ARCHITECTURE.md`
- `UIWidgets.GameEngineCoreIntegration.Editor` assembly — optional CentralAuthoring integration, gated by the `HOMAM_GEC` define constraint

### Changed
- Decoupled core `UIWidgets.Editor` from `GameEngineCore.Editor`: dropped the hard asmdef reference; moved the CentralAuthoring plugin into the optional integration assembly above; removed a dead `using` (`UIWidgetsWindow`) and the `[CentralAuthoringExempt]` marker (`UIWidgetsAssetScriptable`). Core Runtime + Editor now build with zero GameEngineCore dependency.
- Relicensed to MIT ahead of publishing to OpenUPM

### Moved
- `Runtime/Tests/InputDialogTest.cs` → `Samples~/InputDialogDemo/` — it was a manual demo (not an NUnit test) and shouldn't ship in the compiled Runtime assembly

### Known issues
- `PackageIntegrationManifest.asset` at the package root is a HOMAM-internal CentralAuthoring artifact (a `GameEngineCore.Editor` ScriptableObject). It deserializes only when GameEngineCore is present; excluded from standalone distribution.
- No automated test suite (`Tests/` asmdef) yet
