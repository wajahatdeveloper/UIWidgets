# Sample scenes (UI Widgets)

## UI Widgets Demo

Package Manager sample path: `Samples~/UIWidgets Demo`

Play Mode gallery with section tabs (Modals / Buttons / Lists / Feedback / Layout). Covers Dialog, InputDialog, Fader, LineMessage, LoadingPanel, WaitPanel, ModalService, ButtonX, ScrollList, UITabs, Toast, ContextMenu, PopupText, LayoutX.

| | |
|--|--|
| Scene | `UIWidgetsDemo.unity` |
| Scripts | `DemoGalleryBootstrap`, `*Test.cs`, `UIWidgetsDemoImgui` |

## Setup

- **Foundation Platform** installed
- **Input System:** Project Settings → Player → Active Input Handling = Input System Package **or** Both
- **uGUI:** present via `com.unity.ugui`
- **Domain Reload:** leave **enabled**
- **URP** recommended
- Do **not** install a second UniTask package

## How to import

1. Package Manager → UI Widgets → Samples → **Import** “UI Widgets Demo”
2. Open `UIWidgetsDemo.unity`
3. Enter Play Mode — left-rail tabs **M B L F X** (demo objects live in the scene; Bootstrap toggles section visibility)

Note: folders named `Samples~` are hidden in the Project window by design — use Package Manager → Samples to import.

## Related

- [Sample README](Samples~/UIWidgets%20Demo/README.md)
- [Documentation index](Documentation~/index.md)
