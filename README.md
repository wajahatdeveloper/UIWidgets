# UI Widgets

Free Unity UGUI widget library for the **AetherNexus** toolkit (`com.aethernexus.uiwidgets`). Modals, lists, buttons, layout, and editor tooling — built on Foundation Platform.

**Publisher:** [AetherNexus](https://aethernexus.online) · **Support:** wajahatdeveloperqs@gmail.com  
**Unity:** 6000.3.10f1+ · **URP** recommended · **License:** [MIT](LICENSE.md)  
**Requires:** [Foundation Platform](https://aethernexus.online) (`com.aethernexus.foundationplatform`) — UniTask is embedded there; see [Third-Party Notices.txt](Third-Party%20Notices.txt)

## What's inside

| Area | What you get |
|------|----------------|
| **Panels & modals** | `Dialog`, `InputDialog`, `LoadingPanel`, `WaitPanel`, `Fader` with fluent builders |
| **ButtonX** | Enhanced UGUI button: states, long-press, double-click, hold-repeat, toggle |
| **ScrollList** | Virtualized pooled list with `ObservableList<T>` binding |
| **Navigation & chrome** | Context menus, tabs, sliders, card UI, toasts, tooltips |
| **Graphics & effects** | Procedural primitives, gradients, soft masks, UI effects |
| **LayoutX** | Single-component flow / grid layout |
| **Editor** | UI Widgets window, GameObject create menus, Text→TMP migration, ButtonX upgrade |

Docs index: [Documentation~/index.md](Documentation~/index.md)

## Install (Asset Store UPM)

1. Install **Foundation Platform** first (Package Manager → **My Assets** → Download / Import).
2. Package Manager → **My Assets** → **UI Widgets** → Download / Import.
3. Project Settings → Player → **Active Input Handling** = Input System Package **or** Both.
4. Confirm **uGUI** is present (`com.unity.ugui` — included in typical URP templates).
5. Optional: Package Manager → Samples → import **UI Widgets Demo**.

**Do not** install Cysharp UniTask separately. Foundation Platform embeds UniTask; a second UniTask package collides on the `UniTask` assembly name.

## Dependencies

| Dependency | How provided |
|------------|----------------|
| `com.aethernexus.foundationplatform` | Asset Store / Package Manager (required) |
| UniTask (MIT) | Embedded in Foundation Platform |
| `com.unity.inputsystem` | Declared in `package.json` |
| `com.unity.ugui` | Declared in `package.json` (includes TextMeshPro on Unity 6) |

## Quick usage

```csharp
using AetherNexus.UIWidgets;

Dialog.Create("Delete this item?")
    .WithLayout(Dialog.Layout.YesNo)
    .OnYes(() => Delete())
    .ShowWithLayout();

Toast.Create("Saved")
    .WithDuration(3f)
    .WithColor(ToastColor.Green)
    .AtPosition(ToastPosition.TopRight)
    .Show(); // requires ToastUI in the scene (Singletons → Toast Message Canvas)

var items = new[] {
    new ScrollListItemData("Alpha", "First"),
    new ScrollListItemData("Beta", "Second"),
};
scrollList.SetDataSource(items);
```

Runtime types live under `AetherNexus.UIWidgets`. Editor tools under `AetherNexus.UIWidgets.Editor`.

## Useful menus

| Menu | Purpose |
|------|---------|
| **Window → UIWidgets → UI Widgets...** | Browse / instantiate widget prefabs (includes **Default UI** stock creates) |
| **GameObject → UI (Canvas) → …** | Create unique widgets flat under Unity UI (Panel Base, ButtonX, ScrollList, Singletons, …) |
| **Tools → UIWidgets → Settings...** | Package settings |
| **Tools → UIWidgets → Scene Picker Enabled** | Scene pick toggle |

## Assemblies

| Assembly | Role |
|----------|------|
| `UIWidgets.Runtime` | Runtime widgets |
| `UIWidgets.Editor` | Editor tooling |
| `UIWidgets.GameEngineCoreIntegration.Editor` | Optional Central Authoring plugin — compiles only when scripting define `HOMAM_GEC` is present (GameEngineCore installs). Inactive for a standalone UI Widgets project. |

## Package Integration Manifest

`PackageIntegrationManifest.asset` registers this package with **GameEngineCore Central Authoring** when that product is installed. Optional metadata for the wider AetherNexus hub — not required for Dialog, ButtonX, lists, or LayoutX.

## Compatibility

- **Unity** 6000.3.10f1+
- **URP** recommended (Unity 6 default)
- **Foundation Platform** required
- **Fast Enter Play Mode** (Domain Reload disabled): **not supported**. Keep Domain Reload enabled.

## Samples

Import **UI Widgets Demo** from Package Manager Samples. Details: [SAMPLES.md](SAMPLES.md)

## Support

- Website: [aethernexus.online](https://aethernexus.online)
- Email: wajahatdeveloperqs@gmail.com
- Changes: [CHANGELOG.md](CHANGELOG.md)

## Version

**1.0.0** — public API; breaking changes bump MAJOR.
