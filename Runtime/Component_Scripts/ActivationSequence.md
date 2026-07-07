## ActivationSequence

A lightweight controller that activates items (pages/panels/GameObjects) in sequence with optional wrap-around, delays, and transition hooks.

### Installation
- Add the `ActivationSequence` component to a parent GameObject.
- Either let it auto-populate its `sequence` from children, or enable `customSequence` and assign items manually.

### Quick Start
1) Leave `customSequence` disabled to auto-collect direct children.
2) Optionally enable `includeInactiveChildren` if some children start inactive.
3) Keep `autoInitialize` enabled to activate only the first element on enable.
4) Call `Next()`/`Previous()` from UI Buttons or other scripts.

```csharp
public class Pager : MonoBehaviour
{
	[SerializeField] private ActivationSequence sequence;

	public void OnNext() => sequence.Next();
	public void OnPrev() => sequence.Previous();
	public void OnShowFirst() => sequence.GoToFirst();
}
```

### Serialized Fields (Inspector)
- Source
  - `customSequence`: If disabled, `sequence` auto-populates from this transform's direct children.
  - `includeInactiveChildren`: Include inactive children in auto-population.
- Behavior
  - `autoInitialize`: On enable, show the first item and hide others (if `keepOnlyOneActive`).
  - `wrapAround`: When at ends, continue from the opposite side.
  - `keepOnlyOneActive`: If true, deactivates the previous item when showing the next.
  - `invokeEndOnlyWhenPastLast`: If true, `OnSequenceEnd` fires only when attempting to advance past the last.
- Transition
  - `activationDelay`: Delay between exit of current and enter of next (seconds).
- Sequence
  - `sequence`: Manually ordered list of GameObjects when `customSequence` is enabled.

### Public API
- Properties
  - `int CurrentIndex` – current shown index
  - `int Count` – number of items
  - `bool CanNext` / `bool CanPrevious`
- Navigation
  - `void Next()` / `void Previous()`
  - `void Show(int index)`
  - `void GoToFirst()` / `void GoToLast()`
  - `void ResetSequence()` – re-applies initial state

### Events
- `OnSequenceStart` – after initialization completes on enable/reset
- `OnSequenceEnd` – when reaching the end (see `invokeEndOnlyWhenPastLast`)
- `OnIndexChanged(int index)` – after index changes
- `OnShown(int index)` – when a new item becomes active
- `OnHidden(int index)` – when the previous item is hidden
- `OnTransition(int from, int to)` – when transitioning between items

### Transitions with IActivatable
Implement `IActivatable` on any item to get enter/exit callbacks around activation.

```csharp
public class MyPanelTransition : MonoBehaviour, IActivatable
{
	public void OnEnter()
	{
		// Play show animation, enable input, etc.
	}

	public void OnExit()
	{
		// Play hide animation, disable input, etc.
	}
}
```

Order of operations in a transition:
1) `OnTransition(from, to)`
2) If `keepOnlyOneActive` and from exists: call `OnExit()` on from, wait `activationDelay`, deactivate from, then `OnHidden(from)`
3) Activate `to` (if inactive)
4) Call `OnEnter()` on `to`, then `OnShown(to)`
5) Update `CurrentIndex`, then `OnIndexChanged(CurrentIndex)`

### Context Menu (Inspector)
- `Populate From Children` – Rebuilds `sequence` from direct children, honoring `includeInactiveChildren`.
- `Validate & Clean Sequence` – Removes null entries and clamps `CurrentIndex`.

### Common Setups
- Tabs/Pages:
  - `keepOnlyOneActive = true`, `wrapAround = false`, `activationDelay = 0–0.2`
- Carousel:
  - `wrapAround = true`, optional small `activationDelay`
- Stacked Panels (allow multiple active):
  - `keepOnlyOneActive = false` (the `CurrentIndex` follows the first active or ensures target active)

### Notes
- If a new `Show()` is called mid-transition, the current transition is stopped and the new one begins.
- To build your own animations without `IActivatable`, you can still use `activationDelay` as a simple stagger.


