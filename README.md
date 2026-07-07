# UI Widgets

General-purpose Unity UGUI widget library. Built on [FoundationPlatform](https://openupm.com/packages/com.homam.foundationplatform/).

## What's inside

- **Panels & modals** — `PanelBase` (draggable), singleton `Dialog` / `InputDialog` / `LoadingPanel` / `WaitPanel` / `Fader`, all with fluent builders
- **ButtonX** — enhanced UGUI button: 5 visual states, long-press / double-click / hold-repeat / toggle, color-tint or sprite-swap
- **ScrollableList** — virtualized, pooled list with reactive `ObservableList<T>` binding, filtering, sorting
- **Context menu** — `ContextMenuWidget` with smart viewport-clamped placement
- **Tabs** — `UITabs` + keyboard `TabNavigationHelper`
- **Sliders** — `RangeSlider`, `MinMaxSlider`, `BoxSlider`, `RadialSlider`, `Stepper`
- **Card UI** — `CardStack2D`
- **Notifications / focus** — static `Toast` (8 colors × 9 positions), `DefaultFocus`, `AutoClick`
- **Procedural graphics** — `UICircle`, `UIPolygon`, `UISquircle`, `UILineRenderer`, `UIGridRenderer`, `DiamondGraph`
- **Effects & masking** — `UIGradient`, `UIShine`, `UIFlip`, `TeleType`, `UISoftMask`, tooltips
- **LayoutX** — in-house single-component layout engine (Compact flow / Grid), replaces vendored EasyLayout
- **Editor tooling** — UI Widgets Window (browse/instantiate widget prefabs), ScenePicker, Text→TMP migration, ButtonX upgrade tool

## Install

Released standalone via [OpenUPM](https://openupm.com/) at `com.homam.uiwidgets`:

```
openupm add com.homam.uiwidgets
```

Pulls its `com.homam.foundationplatform` + UniTask dependencies from the OpenUPM scoped registry automatically. See [openupm.com/docs/getting-started.html](https://openupm.com/docs/getting-started.html).

## Dependencies

- `com.homam.foundationplatform` 1.0.0
- `com.cysharp.unitask` 2.3.3
- `com.unity.inputsystem` 1.18.0
- `com.unity.ugui` 2.0.0 (includes TextMeshPro on Unity 6)

## Quick usage

```csharp
// Dialog
Dialog.Create("Delete this item?")
    .WithLayout(Dialog.Layout.YesNo)
    .OnYes(() => Delete())
    .Show();

// Toast
Toast.Prepare().WithDuration(3f).WithColor(ToastColor.Green).AtPosition(TopRight).Show();

// Virtualized list bound to a reactive source
scrollableList.SetDataSource(observableItems, x => (x.Name, x.Subtitle, x.Icon));
```

## Assemblies

| Assembly | Folder | Notes |
|---|---|---|
| `UIWidgets.Runtime` | `Runtime/` | refs TextMeshPro, Input System, UniTask, FoundationPlatform.Runtime |
| `UIWidgets.Editor` | `Editor/` | editor tooling; refs FoundationPlatform.Editor |
| `UIWidgets.GameEngineCoreIntegration.Editor` | `Editor/Integration/GameEngineCore/` | **optional** — compiles only when `HOMAM_GEC` is defined (i.e. inside the HOMAM project). Contributes a CentralAuthoring workflow. Dormant in a standalone install. |

## License

[MIT](LICENSE.md) — free to use and modify, keep the copyright notice, don't represent it as your own work.
