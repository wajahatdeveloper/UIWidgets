using System.Collections;
using FoundationPlatform.FrameworkInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UIWidgets
{
	[RequireComponent(typeof(Image))]
	public class ButtonX : Selectable,
		IPointerClickHandler, ISubmitHandler
	{
		public enum ButtonVisualState { Normal, Highlighted, Pressed, Disabled, Selected }
		public enum TransitionType { ColorTint, SpriteSwap }
		[ShowInInspector, PropertyOrder(0)]
		public bool Interactable
		{
			// Delegate to Selectable.interactable so navigation, IsInteractable(), and the
			// EventSystem all agree; keep the legacy raycast + visual behavior on top.
			get => interactable;
			set
			{
				interactable = value;
				if (image != null) { image.raycastTarget = value; }
				_ApplyVisualState(value ? ButtonVisualState.Normal : ButtonVisualState.Disabled);
			}
		}

		[ShowInInspector, PropertyOrder(0)]
		public string Text
		{
			get => _serializedText;
			set
			{
				_serializedText = value ?? string.Empty;
				if (textMesh) { textMesh.text = _serializedText; }
			}
		}

		[SerializeField, HideInInspector] private string _serializedText = "";

		// Renamed from "transition" to avoid hiding Selectable.transition. FormerlySerializedAs
		// preserves data authored on existing prefabs/scene instances.
		[FoldoutGroup("Transitions"), PropertyOrder(1)]
		[FormerlySerializedAs("transition")]
		public TransitionType visualTransition = TransitionType.ColorTint;
		[FoldoutGroup("Colors"), PropertyOrder(2)] public Color normalColor = Color.white;
		[FoldoutGroup("Colors"), PropertyOrder(2)] public Color highlightColor = Color.cyan;
		[FoldoutGroup("Colors"), PropertyOrder(2)] public Color pressedColor = Color.gray;
		[FoldoutGroup("Colors"), PropertyOrder(2)] public Color disabledColor = Color.black;
		[FoldoutGroup("Colors"), PropertyOrder(2)] public Color selectedColor = new Color(0.9f, 0.9f, 1f, 1f);

		[FoldoutGroup("Sprites"), PropertyOrder(3)] public Sprite normalSprite;
		[FoldoutGroup("Sprites"), PropertyOrder(3)] public Sprite highlightSprite;
		[FoldoutGroup("Sprites"), PropertyOrder(3)] public Sprite pressedSprite;
		[FoldoutGroup("Sprites"), PropertyOrder(3)] public Sprite disabledSprite;
		[FoldoutGroup("Sprites"), PropertyOrder(3)] public Sprite selectedSprite;

		[InlineEditor(InlineEditorObjectFieldModes.Foldout), PropertyOrder(4)]
		public TextMeshProUGUI textMesh;

		[InlineEditor(InlineEditorObjectFieldModes.Foldout), PropertyOrder(5)]
		public new Image image;

		[PropertyOrder(6)] public Graphic[] extraGraphicsForTint;

		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnClicked;
		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnSelected;
		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnDeselected;
		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnLongPressed;
		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnDoubleClicked;
		[FoldoutGroup("Events"),PropertyOrder(7)] public UnityEvent OnHoldRepeated;

		private bool isPointerDown = false;
		private bool isSelected = false;
		private float lastPointerDownTime;
		private float lastClickTime = -1f;
		private Coroutine _longPressCoroutine;
		private Coroutine _holdRepeatCoroutine;

		[FoldoutGroup("Interaction"), PropertyOrder(8)] public bool enableDoubleClick = true;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public float doubleClickInterval = 0.25f;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public bool enableLongPress = true;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public float longPressThreshold = 0.5f;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public bool enableHoldRepeat = false;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public float holdRepeatInterval = 0.2f;
		[FoldoutGroup("Interaction"), PropertyOrder(8)] public float clickCooldownSeconds = 0.05f;

		[FoldoutGroup("States"), PropertyOrder(9)]
		public bool IsLoading
		{
			get => _isLoading;
			set
			{
				_isLoading = value;
				_ApplyVisualState(_isLoading ? ButtonVisualState.Disabled :
					(IsInteractable() ? ButtonVisualState.Normal : ButtonVisualState.Disabled));
			}
		}
		[SerializeField, HideInInspector] private bool _isLoading = false;
		[FoldoutGroup("States"), PropertyOrder(9)] public string loadingText = "";
		[FoldoutGroup("States"), PropertyOrder(9)] public bool toggleMode = false;
		[FoldoutGroup("States"), PropertyOrder(9)] public bool IsOn = false;
		[FoldoutGroup("States"), PropertyOrder(9)] public UnityEvent OnToggledOn;
		[FoldoutGroup("States"), PropertyOrder(9)] public UnityEvent OnToggledOff;

		[ShowInInspector, ReadOnly, PropertyOrder(9)]
		public bool IsSelected => isSelected;

		public event System.Action Clicked; // C# event kept for code subscribers

		protected override void Awake()
		{
			base.Awake();
			_AutoAssignImage();
			if (image != null) { targetGraphic = image; }
			// ButtonX drives its own visuals through DoStateTransition; disable Selectable's
			// built-in color/sprite machinery so the two don't fight.
			transition = Transition.None;
			if (textMesh) { textMesh.text = _serializedText; }
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			StopAllCoroutines();
			_longPressCoroutine = null;
			_holdRepeatCoroutine = null;
			isPointerDown = false;
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			_AutoAssignRefsIfNeeded();
			if (image != null && targetGraphic != image) { targetGraphic = image; }
			if (transition != Transition.None) { transition = Transition.None; }
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			_AutoAssignImage();
			// Only write when the value actually changes. OnValidate fires on scene-open and on every
			// script recompile; unconditional writes here (or via _ApplyVisualState pushing onto the
			// child Image/TMP) would mark the scene dirty even when nothing changed.
			if (image != null && targetGraphic != image) { targetGraphic = image; }
			if (transition != Transition.None) { transition = Transition.None; }
			if (textMesh && textMesh.text != _serializedText) { textMesh.text = _serializedText; }
			if (image)
			{
				_ApplyVisualState(IsInteractable() ? ButtonVisualState.Normal : ButtonVisualState.Disabled);
			}
		}
#endif

		private void _AutoAssignImage()
		{
			if (!image) { image = GetComponent<Image>(); }
		}

		private void _AutoAssignRefsIfNeeded()
		{
			_AutoAssignImage();
			if (!textMesh) { textMesh = GetComponentInChildren<TextMeshProUGUI>(true); }
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!isActiveAndEnabled || !IsInteractable() || IsLoading) { return; }
			if (eventData != null && eventData.button != PointerEventData.InputButton.Left) { return; }

			// Evaluate the double-click window BEFORE the cooldown rejection so a genuinely
			// fast second click is recognized as a double-click instead of being swallowed.
			if (enableDoubleClick && lastClickTime >= 0f && Time.unscaledTime - lastClickTime <= doubleClickInterval)
			{
				OnDoubleClicked?.Invoke();
				lastClickTime = -1f;
				return;
			}

			if (Time.unscaledTime - lastClickTime < clickCooldownSeconds) { return; }

			lastClickTime = Time.unscaledTime;

			if (toggleMode)
			{
				SetIsOn(!IsOn);
			}

			OnClicked?.Invoke();
			Clicked?.Invoke();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData); // lets Selectable enter the Pressed state -> DoStateTransition
			if (!isActiveAndEnabled || !IsInteractable() || IsLoading) { return; }
			isPointerDown = true;
			lastPointerDownTime = Time.unscaledTime;
			if (enableLongPress) { _longPressCoroutine = StartCoroutine(_CoCheckLongPress()); }
			if (enableHoldRepeat) { _holdRepeatCoroutine = StartCoroutine(_CoHoldRepeat()); }
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			base.OnPointerUp(eventData);
			if (!isActiveAndEnabled || !IsInteractable() || IsLoading) { return; }
			isPointerDown = false;
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
		}

		public void OnSubmit(BaseEventData eventData)
		{
			if (!isActiveAndEnabled || !IsInteractable() || IsLoading) { return; }
			if (Time.unscaledTime - lastClickTime < clickCooldownSeconds) { return; }
			lastClickTime = Time.unscaledTime;
			if (toggleMode)
			{
				SetIsOn(!IsOn);
			}
			OnClicked?.Invoke();
			Clicked?.Invoke();
		}

		public override void OnSelect(BaseEventData eventData)
		{
			base.OnSelect(eventData);
			isSelected = true;
			OnSelected?.Invoke();
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			isSelected = false;
			OnDeselected?.Invoke();
		}

		private IEnumerator _CoCheckLongPress()
		{
			while (isPointerDown && enableLongPress)
			{
				if (Time.unscaledTime - lastPointerDownTime >= longPressThreshold)
				{
					if (_holdRepeatCoroutine != null)
					{
						StopCoroutine(_holdRepeatCoroutine);
						_holdRepeatCoroutine = null;
					}
					OnLongPressed?.Invoke();
					yield break;
				}
				yield return null;
			}
		}

		private IEnumerator _CoHoldRepeat()
		{
			var wait = new WaitForSecondsRealtime(Mathf.Max(0.01f, holdRepeatInterval));
			yield return wait;
			while (isPointerDown && enableHoldRepeat)
			{
				OnHoldRepeated?.Invoke();
				yield return wait;
			}
		}

		// Single visual driver. Selectable calls this on every selection-state change (pointer,
		// keyboard/gamepad nav, submit); transition == None means base does no graphics itself.
		protected override void DoStateTransition(SelectionState state, bool instant)
		{
			if (IsLoading || !IsInteractable())
			{
				_ApplyVisualState(ButtonVisualState.Disabled);
				return;
			}
			// A toggled-on (or code-selected) button keeps its Selected look even when it isn't
			// the EventSystem's current selection.
			if ((toggleMode && IsOn) || isSelected)
			{
				_ApplyVisualState(ButtonVisualState.Selected);
				return;
			}
			switch (state)
			{
				case SelectionState.Highlighted: _ApplyVisualState(ButtonVisualState.Highlighted); break;
				case SelectionState.Pressed:     _ApplyVisualState(ButtonVisualState.Pressed); break;
				case SelectionState.Selected:    _ApplyVisualState(ButtonVisualState.Selected); break;
				case SelectionState.Disabled:    _ApplyVisualState(ButtonVisualState.Disabled); break;
				default:                          _ApplyVisualState(ButtonVisualState.Normal); break;
			}
		}

		private void _ApplyVisualState(ButtonVisualState state)
		{
			if (!image) { return; }
			if (!IsInteractable()) { state = ButtonVisualState.Disabled; }
			if (IsLoading) { state = ButtonVisualState.Disabled; }

			if (visualTransition == TransitionType.ColorTint)
			{
				var target = _ColorFor(state);
				_ApplyColor(target);
			}
			else if (visualTransition == TransitionType.SpriteSwap)
			{
				var sprite = _SpriteFor(state);
				if (image.overrideSprite != sprite) { image.overrideSprite = sprite; }
			}
		}

		private void _ApplyColor(Color color)
		{
			if (!image.color.Equals(color)) { image.color = color; }
			if (extraGraphicsForTint != null)
			{
				for (int i = 0; i < extraGraphicsForTint.Length; i++)
				{
					var g = extraGraphicsForTint[i];
					if (g && !g.color.Equals(color)) { g.color = color; }
				}
			}
		}

		private Color _ColorFor(ButtonVisualState state)
		{
			switch (state)
			{
				case ButtonVisualState.Highlighted: return highlightColor;
				case ButtonVisualState.Pressed: return pressedColor;
				case ButtonVisualState.Disabled: return disabledColor;
				case ButtonVisualState.Selected: return selectedColor;
				default: return normalColor;
			}
		}

		private Sprite _SpriteFor(ButtonVisualState state)
		{
			switch (state)
			{
				case ButtonVisualState.Highlighted: return highlightSprite ? highlightSprite : normalSprite;
				case ButtonVisualState.Pressed: return pressedSprite ? pressedSprite : normalSprite;
				case ButtonVisualState.Disabled: return disabledSprite ? disabledSprite : normalSprite;
				case ButtonVisualState.Selected: return selectedSprite ? selectedSprite : normalSprite;
				default: return normalSprite;
			}
		}

		[Button]
		private void AssignRefs()
		{
			_AutoAssignRefsIfNeeded();
		}

		public void SetText(string value)
		{
			Text = value;
		}

		public void SetInteractable(bool value)
		{
			Interactable = value;
		}

		public void Click()
		{
			if (!isActiveAndEnabled || !IsInteractable() || IsLoading) { return; }
			OnClicked?.Invoke();
			Clicked?.Invoke();
		}

		public void SetIsOn(bool value)
		{
			if (!toggleMode) { return; }
			IsOn = value;
			if (IsOn) { OnToggledOn?.Invoke(); } else { OnToggledOff?.Invoke(); }
			_ApplyVisualState(IsOn ? ButtonVisualState.Selected : ButtonVisualState.Normal);
		}
	}
}
