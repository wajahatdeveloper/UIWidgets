# UI Widgets Demo

Play Mode sample for `com.aethernexus.uiwidgets`.

## Requires

- Unity **6000.3.10f1+** (URP recommended)
- **Foundation Platform** installed
- **Input System** active (Active Input Handling = Input System Package or Both)
- Domain Reload **enabled**
- All demo widgets / singletons already placed in `UIWidgetsDemo.unity` (disabled until their section tab is shown)

## How to run

1. Package Manager → UI Widgets → Samples → **Import** “UI Widgets Demo” (or open the imported scene under `Assets/Samples`)
2. Open `UIWidgetsDemo.unity`
3. Enter Play Mode — left rail tabs: **M** Modals · **B** Buttons · **L** Lists · **F** Feedback · **X** Layout

## DemoHarness

`DemoGalleryBootstrap` on **DemoHarness**:

- Resolves scene instances (incl. inactive)
- Wires harness scripts
- Enables only the active section’s roots

Optional Inspector: assign **Scene instances**, **Section roots**, and **Harness wiring** if auto-resolve is wrong.

Context menu → **Resolve Scene + Apply Visibility**.

`ModalServiceTest.demoPanel` should point at **ModalPanel** with **Is Modal** on.

## Section map

| Tab | What you exercise | Demo objects |
|-----|-------------------|--------------|
| **M** | Dialog, InputDialog, Fader, LineMessage, Loading, Wait, ModalService | `modalsSectionRoots` (optional) |
| **B** | ButtonX, ButtonXToggleGroup | `Demo_ButtonX` + `Demo_ButtonXToggle` (3 mutual-exclusion buttons) |
| **L** | ScrollList, UITabs | list roots enabled |
| **F** | Toast, ContextMenu, PopupText | feedback singletons enabled |
| **X** | LayoutX | LayoutX root enabled |

## Scripts

| Script | Role |
|--------|------|
| `DemoGalleryBootstrap` | Section tabs + show/hide scene roots |
| `*Test.cs` | Play Mode harnesses (IMGUI + Context Menu) |
| `UIWidgetsDemoImgui` | Shared rail layout |
