# UI Widgets Demo

Play Mode sample exercising the modal / overlay widgets placed in `UIWidgetsDemo`.

## Requires

- Unity **6000.3.10f1+** (URP recommended)
- **Foundation Platform** installed
- **Input System** active (Active Input Handling = Input System Package or Both)
- Domain Reload **enabled**
- Scene must contain the singleton instances: Dialog, InputDialog, Fader, LineMessage, Loading, Wait, ModalService

## How to run

1. Package Manager → UI Widgets → Samples → **Import** “UI Widgets Demo”
2. Open `UIWidgetsDemo.unity`
3. Add a GameObject (e.g. `DemoHarness`) and attach the test scripts you want
4. For **ModalServiceTest**, also place a **Panel** (`GameObject → UIWidgets → Containers → Panel`) and assign it to `demoPanel` (enable **Is Modal** if using `PanelBase.Show`)
5. Enter Play Mode — use the left IMGUI buttons or component Context Menus

Optional: **Window → DebugX Console...**

## Scripts

| Script | Tests |
|--------|-------|
| `DialogTest` | Ok / YesNo / OkCancel / YesNoCancel |
| `InputDialogTest` | Basic / Validation / Enter·Esc dialog shortcuts |
| `FaderTest` | To black / From black / Round-trip / Cancel |
| `LineMessageTest` | Title / Message only / Burst |
| `LoadingPanelTest` | Show / Timed progress bar / Hide |
| `WaitPanelTest` | Show / Hide / Counted ± |
| `ModalServiceTest` | Show / Hide / PanelBase.Show |

Each harness draws a **narrow left-rail** IMGUI strip (`UIWidgetsDemoImgui`, 128px) so the center stays clear for widgets. No IMGUI hotkeys — click the buttons (or Context Menu).

## Files

- `UIWidgetsDemo.unity` — scene with widget singletons
- `*Test.cs` — Play Mode harnesses (`AetherNexus.UIWidgets`)
