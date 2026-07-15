# Sample scenes (UI Widgets)

## UI Widgets Demo

Package Manager sample path: `Samples~/UIWidgets Demo`

Play Mode scene with widget singletons plus harness scripts for Dialog, InputDialog, Fader, LineMessage, LoadingPanel, WaitPanel, and ModalService.

| | |
|--|--|
| Scene | `UIWidgetsDemo.unity` |
| Scripts | `*Test.cs` (OnGUI + hotkeys + Context Menu) |

## Setup

- **Foundation Platform** installed
- **Input System:** Project Settings → Player → Active Input Handling = Input System Package **or** Both
- **uGUI:** present via `com.unity.ugui` (typical URP templates)
- **Domain Reload:** leave **enabled** (Fast Enter Play Mode is not supported)
- **URP** recommended
- Do **not** install a second UniTask package

## How to import

1. Package Manager → UI Widgets → Samples → **Import** “UI Widgets Demo”
2. Open `UIWidgetsDemo.unity`
3. Attach the `*Test` scripts to a harness GameObject (or use the scene’s setup)
4. For ModalService: assign a `PanelBase` to `ModalServiceTest.demoPanel`
5. Enter Play Mode

Note: folders named `Samples~` are hidden in the Project window by design — use Package Manager → Samples to import.

## Related

- [Sample README](Samples~/UIWidgets%20Demo/README.md)
- [Documentation index](Documentation~/index.md)
