# UI Widgets Architecture

General-purpose Unity UGUI widget library. Depends only on `com.aethernexus.foundationplatform` (+ TextMeshPro / Input System / UniTask via Foundation Platform). Game-specific widgets are **not** part of this package — they live in gameplay frameworks that consume these widgets.

---

## Assemblies

| Assembly | Folder | References | Notes |
|---|---|---|---|
| `UIWidgets.Runtime` | `Runtime/` | TextMeshPro, Input System, UniTask, FoundationPlatform.Runtime | ships |
| `UIWidgets.Editor` | `Editor/` | FoundationPlatform.Runtime/Editor, UIWidgets.Runtime | ships |
| `UIWidgets.GameEngineCoreIntegration.Editor` | `Editor/Integration/GameEngineCore/` | UIWidgets.Editor, GameEngineCore.Editor

**Namespaces:** `AetherNexus.UIWidgets` (runtime) and `AetherNexus.UIWidgets.Editor` (editor). First-party layout (`LayoutX`) and scene picking (`ScenePicker`) ship in this package.

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

## ScrollList

`ScrollList` (`ScrollList_Scripts/ScrollList.cs`) — virtualized, pooled list.

```csharp
// Stock item prefab (ScrollItemView): bind ScrollListItemData or string
scrollList.SetDataSource(items); // IEnumerable<ScrollListItemData>
scrollList.SetDataSource(observableItems); // ObservableList<T> — reactive auto-sync

// Domain models: subclass ScrollItemView<T> on the item prefab and implement Bind/Unbind
```

- Item prefab must expose `IListItemBinder` — stock `ScrollItemView` handles `ScrollListItemData` / `string`; typed rows use `ScrollItemView<T>`
- **Virtualization:** enable on the component (`useVirtualization`) and pass `useVirtualization: true` to `SetDataSource`; only visible rows + buffer instantiated
- **Pooling:** `Queue<GameObject>` recycled on filter/scroll
- **Ops:** `RemoveItem`, `RemoveItemAt`, `ClearAllItems`, `GetItemAt`, `FilterItems`, `SortItems`, `ScrollToItem`
- **Events:** `OnItemBound`, `OnItemUnbound`, `OnListRefreshed`

---

## Other widgets

| Widget | File | Notes |
|---|---|---|
| `SpecialDialog` | `Panel_Scripts/SpecialDialog.cs` | subclass of `Dialog`; override `BuildLayout` + show/hide hooks |
| `ContextMenuWidget` | `Component_Scripts/ContextMenuWidget.cs` | singleton; `Show(ContextMenuRequest)`; viewport-clamped placement (`ScreenPosition` / `TargetRect`, preferred direction), `HideAll()` |
| `UITabs` | `Scripts/UITabs.cs` | `OnClick_TabButton`, `RebuildTabsFromParents`, `SelectTabByName`; `TabNavigationHelper` for Tab/Shift-Tab nav |
| Sliders | `Sliders_Scripts/` | `RangeSlider`, `MinMaxSlider`, `BoxSlider`, `RadialSlider`, `Stepper` / `StepperSide` |
| `CardStack2D` | `CardUI_Scripts/` | card deck; exponential spacing; lerp to target |
| `Toast` | `Component_Scripts/Toast.cs` | static; `Toast.Create(text).WithDuration().WithColor()/WithSeverity().AtPosition().ClickToDismiss().Replace().Show()`; FIFO queue on `ToastUI`; auto text contrast; optional `WithIcon`; requires a `ToastUI` instance in the scene |
| `PopupText` | `Scripts/PopupText.cs` | floating text spawner; requires item prefab + container |
| `DefaultFocus`, `AutoClick`, `AlphaButtonClickMask` | `Utility_Scripts/` | focus/click helpers, alpha raycast filter |

---

## Procedural graphics & effects

- **Primitives** (`Primitive_Scripts/`): `UICircle`, `UIPolygon`, `UISquircle`, `UILineRenderer`, `UIGridRenderer`, `UICornerCut`, `DiamondGraph`
- **Effects** (`UIEffects/`): `UIGradient`, `UIShine`, `UIFlip`, `UICanvasParticles`, `TeleType` (typewriter)
- **Masking:** `UISoftMask` (soft alpha), `UIRaycastAlphaMask` (raycast filter)
- **Tooltips** (`ToolTips/`): `ToolTip` + `TooltipTrigger` (IPointerEnter/Exit)

---

## LayoutX (layout engine)

`LayoutX` (`Runtime/Layout/LayoutX.cs`) — single-component flow/grid layout engine.

- Modes: `Compact` (flow, wraps by element size) and `Grid` (uniform cells)
- **Main Axis:** `Horizontal` (flow right, wrap to next row) or `Vertical` (flow down, wrap to next column)
- Axis-relative constraints (`Flexible` / `MaxItemsPerLine` / `MaxLines`), line + cross alignment, `childAlignment` for content-block anchor
- Child size: `SizeControl.None` measures/keeps rect size; `Preferred` drives preferred size; optional `useChildRectSize`, `resetChildRotation`
- Parity: `reverseArrangement`, `childForceExpandMain` / `childForceExpandCross` (distribute free main space; stretch to line cross; Grid Flexible grows cells)
- Preferred sizes are ContentSizeFitter-safe: unconstrained one-line preferred on the main axis, wrap rebuild on the cross axis
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
| ScenePicker | `Editor/ScenePicker/` | scene-view object picking + `AnchorTools` |
| Settings | `Editor/Settings/` | `UIWidgetsSettings` + `UIWidgetsSettingsProvider` (Project Settings page) |
| `TextToTMPMigrationTool` | `Editor/` | migrate legacy `Text` → TMP |
| `UIWidgetsSceneOverlay` | `Editor/` | scene-view overlay |
| Central Authoring plugin | `Editor/Integration/GameEngineCore/` | **optional**, see Assemblies |

---

## Key patterns

- **Fluent builders** — `DialogBuilder`, `ToastBuilder`, `LoadingPanelBuilder`: chained methods + `.Show()`
- **Singleton modals** — `Dialog`, `LoadingPanel`, `ContextMenuWidget` on `SingletonBehaviour<T>`; `ToastUI` on `PersistentSingletonBehaviour<T>` (queued toasts)
- **Object pooling** — `ScrollList` recycles a `Queue<GameObject>` on filter/scroll
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
