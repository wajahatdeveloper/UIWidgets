# UI Widgets Architecture

General-purpose Unity UGUI widget library. Depends only on `com.aethernexus.foundationplatform` (+ TextMeshPro / Input System / UniTask). Game-specific widgets are **not** part of this package — they live in their own gameplay frameworks and consume these widgets.

---

## Assemblies

| Assembly | Folder | References | Notes |
|---|---|---|---|
| `UIWidgets.Runtime` | `Runtime/` | TextMeshPro, Input System, UniTask, FoundationPlatform.Runtime | ships |
| `UIWidgets.Editor` | `Editor/` | FoundationPlatform.Runtime/Editor, UIWidgets.Runtime | ships |
| `UIWidgets.GameEngineCoreIntegration.Editor` | `Editor/Integration/GameEngineCore/` | UIWidgets.Editor, GameEngineCore.Editor | **optional** — `defineConstraints: ["HOMAM_GEC"]` |

**Namespaces:** `UIWidgets` (runtime) and `UIWidgets.Editor` (editor). No third-party namespaces — vendored deps were internalized (EasyLayout → `LayoutX`, Nementic scene picker → `ScenePicker`).

### The optional GameEngineCore integration

Core UIWidgets has **zero** dependency on GameEngineCore. The only coupling — a CentralAuthoring plugin — is isolated in `UIWidgets.GameEngineCoreIntegration.Editor`, gated by the `HOMAM_GEC` scripting define. GameEngineCore sets that define on load (`GameEngineCorePresenceDefine`), so:

- **Inside HOMAM** — GameEngineCore present → `HOMAM_GEC` defined → integration compiles → `UIWidgetsCentralAuthoringPlugin` is discovered via TypeCache and contributes the "Widget Setup" workflow.
- **Standalone install** — no GameEngineCore, no define → the integration assembly is skipped entirely → package compiles clean.

Do not re-add a `GameEngineCore.Editor` reference to the core Editor asmdef — it would reintroduce the hard dependency this split exists to remove.

---

## Base classes

| Class | File | Role |
|---|---|---|
| `WidgetBase` | `Scripts/WidgetBase.cs` | abstract MonoBehaviour base for all widgets |
| `WidgetPanelBase` | `Scripts/WidgetBase.cs` | concrete panel subclass; extends `PanelBase` |

---

## Panels & modals

`PanelBase` (`Component_Scripts/PanelBase.cs`) — draggable panel:
- `Show()` / `Hide()` / `IsShown()`
- events `OnBeforeShown`, `OnAfterShown`, `OnBeforeHidden`, `OnAfterHidden`
- drag with canvas-bounds clamping; virtual `OnBeforeShow()` / `OnAfterShow()` hooks

Singleton modals (all via `FoundationPlatform`'s `SingletonBehaviour<T>`):

| Class | Behavior |
|---|---|
| `Dialog` | Ok / OkCancel / YesNo / YesNoCancel layouts; icons; Enter/Esc shortcuts; `DialogBuilder` fluent API |
| `InputDialog` | text-input dialog with `InputValidation` (required / min / max length) |
| `LoadingPanel` | loading overlay; auto-close timer; progress bar; `LoadingPanelBuilder` |
| `WaitPanel` | generic activity indicator |
| `Fader` | screen fade transitions |
| `LineMessage` | line-based message broadcasts |

`Dialog.Layout`: `Ok, OkCancel, YesNo, YesNoCancel`. `Dialog.DialogIcon`: `None, Info, Warning, Error, Custom`.

---

## ButtonX

`ButtonX` (`Scripts/ButtonX.cs`) — enhanced UGUI button.

- **Visual states:** `Normal, Highlighted, Pressed, Disabled, Selected`; transitions via `ColorTint` or `SpriteSwap`
- **Events:** `OnClicked`/`Clicked`, `OnSelected`/`OnDeselected`, `OnLongPressed`, `OnDoubleClicked`, `OnHoldRepeated`, `OnToggledOn`/`OnToggledOff`
- **Settings:** `enableDoubleClick`, `enableLongPress`, `enableHoldRepeat`, `clickCooldownSeconds`, `toggleMode`, `IsOn`
- **Programmatic:** `Click()`, `Select()`, `SetIsOn(bool)`, `SetText(string)`, `SetInteractable(bool)`

Editor: `ButtonXEditor`, `UpgradeToButtonX` (converts stock Buttons).

---

## ScrollableList

`ScrollableList` (`ScrollList_Scripts/ScrollableList.cs`) — virtualized, pooled list.

```csharp
SetDataSource<T>(IEnumerable<T> source, Func<T,(title,subtitle,sprite)> binder)
SetDataSource<T>(ObservableList<T> source, binder)   // reactive auto-sync
```

- Items implementing `IBindableListItem<T>.Bind(data, index)` override the default binder
- **Virtualization:** only visible rows + `virtualizationBuffer` instantiated; recalculated on scroll; `itemHeight`/`itemWidth` auto-measured if 0
- **Pooling:** `Queue<GameObject>` recycled on filter/scroll
- **Ops:** `AddItem`, `RemoveItem`, `RemoveItemAt`, `ClearAllItems`, `GetItemAt`, `FilterItems(text, inTitle, inSubtitle)`, `SortItems(comparison)`, `ScrollToItem(index, smooth)`
- **Events:** `OnItemSelected`, `OnListRefreshed`, `OnItemAdded`, `OnItemRemoved`

---

## Other widgets

| Widget | File | Notes |
|---|---|---|
| `ContextMenuWidget` | `Component_Scripts/ContextMenuWidget.cs` | singleton; `Show(ContextMenuRequest)`; viewport-clamped placement (`ScreenPosition` / `TargetRect`, preferred direction), `HideAll()` |
| `UITabs` | `Scripts/UITabs.cs` | `OnClick_TabButton`, `RebuildTabsFromParents`, `SelectTabByName`; `TabNavigationHelper` for Tab/Shift-Tab nav |
| Sliders | `Sliders_Scripts/` | `RangeSlider`, `MinMaxSlider`, `BoxSlider`, `RadialSlider`, `Stepper` / `StepperSide` |
| `CardStack2D` | `CardUI_Scripts/` | card deck; exponential spacing; lerp to target |
| `Toast` | `Component_Scripts/Toast.cs` | static; `Toast.Prepare().WithDuration().WithColor().AtPosition().Show()`; 8 colors × 9 positions; loaded from Resources |
| `DefaultFocus`, `AutoClick`, `AlphaButtonClickMask` | `Utility_Scripts/` | focus/click helpers, alpha raycast filter |

---

## Procedural graphics & effects

- **Primitives** (`Primitive_Scripts/`): `UICircle`, `UIPolygon`, `UISquircle`, `UILineRenderer`, `UIGridRenderer`, `UICornerCut`, `DiamondGraph`
- **Effects** (`UIEffects/`): `UIGradient`, `UIShine`, `UIFlip`, `UICanvasParticles`, `TeleType` (typewriter)
- **Masking:** `UISoftMask` (soft alpha), `UIRaycastAlphaMask` (raycast filter)
- **Tooltips** (`ToolTips/`): `ToolTip` + `TooltipTrigger` (IPointerEnter/Exit)

---

## LayoutX (layout engine)

`LayoutX` (`Runtime/Layout/LayoutX.cs`) — in-house single-component layout, replaces the vendored EasyLayout package.

- Modes: `Compact` (flow, wraps by element size) and `Grid` (uniform cells), on either axis
- Axis-relative constraints (`MaxItemsPerLine` / `MaxLines`), line + cross alignment, optional child size driving (`Preferred`), rect-size measuring, child rotation reset
- Extend by adding to `LayoutMode`. Editor: `LayoutXEditor`.

---

## Utilities

- `AutoUIRefs` (`Scripts/AutoUIRefs.cs`) — recursive component scan (Button, ButtonX, TMP text/input/dropdown, Image, Toggle, Slider, ScrollRect, MonoBehaviours); options `includeInactive`, `preventDuplicates`, `includeMonoBehaviourScripts`. Editor: `AutoUIRefsEditor`.
- `SafeArea` — adapts rect to device notch/safe area
- `BezierPath` — cubic Bézier spline
- `UIInput` — polling input facade (Input System / legacy backend)

---

## Editor tooling

| Tool | Location | Purpose |
|---|---|---|
| UI Widgets Window | `Editor/UIWidgetsWindow/` | browse & instantiate widget prefabs into the open scene (partial class: `.cs` / `.UI.cs` / `.View.cs`); backed by `UIWidgetsAssetScriptable` |
| ScenePicker | `Editor/ScenePicker/` | scene-view object picking + `AnchorTools` (internalized, was Nementic) |
| Settings | `Editor/Settings/` | `UIWidgetsSettings` + `UIWidgetsSettingsProvider` (Project Settings page) |
| `TextToTMPMigrationTool` | `Editor/` | migrate legacy `Text` → TMP |
| `UIWidgetsSceneOverlay` | `Editor/` | scene-view overlay |
| CentralAuthoring plugin | `Editor/Integration/GameEngineCore/` | **optional**, see Assemblies |

---

## Key patterns

- **Fluent builders** — `DialogBuilder`, `ToastBuilder`, `LoadingPanelBuilder`: chained methods + `.Show()`
- **Singleton modals** — `Dialog`, `LoadingPanel`, `Toast`, `ContextMenuWidget` on `SingletonBehaviour<T>`
- **Object pooling** — `ScrollableList` recycles a `Queue<GameObject>` on filter/scroll
- **Reactive binding** — `ObservableList<T>` subscriptions auto-sync the list on `ItemAdded`/`Removed`/`Cleared`
- **State machine (ButtonX)** — 5 visual states driven by pointer/keyboard events

---

## External dependencies

| Library | Usage |
|---|---|
| TextMeshPro (via `com.unity.ugui`) | `TextMeshProUGUI`, `TMP_InputField`, `TMP_Dropdown` |
| UnityEngine.UI (UGUI) | `Button`, `Image`, `Slider`, `Toggle`, `ScrollRect` |
| UnityEngine.EventSystems | `IPointerClickHandler`, `IDragHandler`, `Selectable` |
| Input System | keyboard/gamepad, with legacy Input fallback (`UIInput`) |
| FoundationPlatform | `SingletonBehaviour<T>`, `ObservableList<T>`, DebugX logging, editor attributes |
