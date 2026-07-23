using System.Collections;
using AetherNexus.FoundationPlatform.AetherInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
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
		
		[ShowInInspector, LabelText("Text (Linked)"), PropertyOrder(1)]
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

		[InlineEditor(InlineEditorObjectFieldModes.Foldout), PropertyOrder(2)]
		public TextMeshProUGUI textMesh;

		[InlineEditor(InlineEditorObjectFieldModes.Foldout), PropertyOrder(2)]
		public new Image image;

		// Renamed from "transition" to avoid hiding Selectable.transition. FormerlySerializedAs
		// preserves data authored on existing prefabs/scene instances.
		// FoldoutGroup order (2nd ctor arg) drives sibling sort vs ungrouped fields — PropertyOrder alone
		// does not lift the group past Order-0 defaults.
		[FoldoutGroup("Transitions", 4), PropertyOrder(4)]
		public TransitionType visualTransition = TransitionType.ColorTint;

		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Color normalColor = Color.white;
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Color highlightColor = new Color(0.92f, 0.95f, 1f);
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Color pressedColor = new Color(0.70f, 0.75f, 0.85f);
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Color disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Color selectedColor = new Color(0.45f, 0.70f, 1f);
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowColorTintFields)), PropertyOrder(5)]
		public Graphic[] extraGraphicsForTint;

		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowSpriteSwapFields)), PropertyOrder(6)]
		public Sprite normalSprite;
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowSpriteSwapFields)), PropertyOrder(6)]
		public Sprite highlightSprite;
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowSpriteSwapFields)), PropertyOrder(6)]
		public Sprite pressedSprite;
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowSpriteSwapFields)), PropertyOrder(6)]
		public Sprite disabledSprite;
		[FoldoutGroup("Transitions", 4), ShowIf(nameof(ShowSpriteSwapFields)), PropertyOrder(6)]
		public Sprite selectedSprite;

		[FoldoutGroup("Interaction", 7), PropertyOrder(7)] public float clickCooldownSeconds = 0.5f;
		[FoldoutGroup("Interaction", 7), PropertyOrder(7)] public bool enableDoubleClick = false;
		[FoldoutGroup("Interaction", 7), ShowIf(nameof(enableDoubleClick)), PropertyOrder(7)]
		public float doubleClickInterval = 0.25f;
		[FoldoutGroup("Interaction", 7), PropertyOrder(7)] public bool enableLongPress = false;
		[FoldoutGroup("Interaction", 7), ShowIf(nameof(enableLongPress)), PropertyOrder(7)]
		public float longPressThreshold = 0.5f;
		[FoldoutGroup("Interaction", 7), PropertyOrder(7)] public bool enableHoldRepeat = false;
		[FoldoutGroup("Interaction", 7), ShowIf(nameof(enableHoldRepeat)), PropertyOrder(7)]
		public float holdRepeatInterval = 0.2f;

		[FoldoutGroup("Events", 8), PropertyOrder(8)] public UnityEvent OnClicked;
		[FoldoutGroup("Events", 8), PropertyOrder(8)] public UnityEvent OnSelected;
		[FoldoutGroup("Events", 8), PropertyOrder(8)] public UnityEvent OnDeselected;
		[FoldoutGroup("Events", 8), ShowIf(nameof(enableDoubleClick)), PropertyOrder(8)]
		public UnityEvent OnDoubleClicked;
		[FoldoutGroup("Events", 8), ShowIf(nameof(enableLongPress)), PropertyOrder(8)]
		public UnityEvent OnLongPressed;
		[FoldoutGroup("Events", 8), ShowIf(nameof(enableHoldRepeat)), PropertyOrder(8)]
		public UnityEvent OnHoldRepeated;

		[FoldoutGroup("States", 9), PropertyOrder(9)]
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
		[FoldoutGroup("States", 9), ShowIf(nameof(IsLoading)), PropertyOrder(9)]
		public string loadingText = "";
		[FoldoutGroup("States", 9), PropertyOrder(9)] public bool toggleMode = false;
		[FoldoutGroup("States", 9), ShowIf(nameof(toggleMode)), PropertyOrder(9)]
		public bool IsOn = false;
		[FoldoutGroup("States", 9), ShowIf(nameof(toggleMode)), PropertyOrder(9)]
		[LabelText("Toggle Image")]
		public Image toggleImage;
		[FoldoutGroup("States", 9), ShowIf(nameof(toggleMode)), PropertyOrder(9)]
		public ButtonXToggleGroup toggleGroup;
		[FoldoutGroup("States", 9), ShowIf(nameof(toggleMode)), PropertyOrder(9)]
		public UnityEvent OnToggledOn;
		[FoldoutGroup("States", 9), ShowIf(nameof(toggleMode)), PropertyOrder(9)]
		public UnityEvent OnToggledOff;

		[ShowInInspector, ReadOnly, FoldoutGroup("States", 9), PropertyOrder(9)]
		public bool IsSelected => isSelected;

		private bool isPointerDown = false;
		private bool isSelected = false;
		private float lastPointerDownTime;
		private float lastClickTime = -1f;
		private Coroutine _longPressCoroutine;
		private Coroutine _holdRepeatCoroutine;

		private bool ShowColorTintFields => visualTransition == TransitionType.ColorTint;
		private bool ShowSpriteSwapFields => visualTransition == TransitionType.SpriteSwap;

		public event System.Action Clicked; // C# event kept for code subscribers

		protected override void Awake()
		{
			base.Awake();
			_AutoAssignImage();
			_DisableChildRaycasts();
			if (image != null) { targetGraphic = image; }
			// ButtonX drives its own visuals through DoStateTransition; disable Selectable's
			// built-in color/sprite machinery so the two don't fight.
			transition = Transition.None;
			if (textMesh) { textMesh.text = _serializedText; }
			_SyncToggleImage();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (toggleGroup != null) { toggleGroup.Register(this); }
		}

		protected override void OnDisable()
		{
			if (toggleGroup != null) { toggleGroup.Unregister(this); }
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
			_DisableChildRaycasts();
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
			_SyncToggleImage();
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
			_DisableChildRaycasts();
		}

		/// <summary>
		/// Child graphics must not raycast — otherwise pointer enter/exit flickers when moving
		/// between the button Image and TMP/toggle icon, and hover feels broken.
		/// </summary>
		private void _DisableChildRaycasts()
		{
			if (textMesh && textMesh.raycastTarget) { textMesh.raycastTarget = false; }
			if (toggleImage && toggleImage.raycastTarget) { toggleImage.raycastTarget = false; }
			if (extraGraphicsForTint == null) { return; }
			for (int i = 0; i < extraGraphicsForTint.Length; i++)
			{
				var g = extraGraphicsForTint[i];
				if (g && g != image && g.raycastTarget) { g.raycastTarget = false; }
			}
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
			// Drag-off: stop press tracking so long-press/hold don't keep running, and let
			// Selectable's exit transition reach Normal/Highlighted instead of staying Pressed.
			isPointerDown = false;
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

			// Toggle-on keeps Selected look, but still show Pressed while held.
			// Do not lock on isSelected — EventSystem leaves the button selected after click,
			// which would swallow Highlighted/Normal on hover and pointer-exit.
			if (toggleMode && IsOn)
			{
				_ApplyVisualState(state == SelectionState.Pressed
					? ButtonVisualState.Pressed
					: ButtonVisualState.Selected);
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
			if (image)
			{
				if (!IsInteractable()) { state = ButtonVisualState.Disabled; }
				if (IsLoading) { state = ButtonVisualState.Disabled; }

				if (visualTransition == TransitionType.ColorTint)
				{
					_ApplyColor(_ColorFor(state));
				}
				else if (visualTransition == TransitionType.SpriteSwap)
				{
					var sprite = _SpriteFor(state);
					if (image.overrideSprite != sprite) { image.overrideSprite = sprite; }
				}
			}

			_SyncToggleImage();
		}

		private void _SyncToggleImage()
		{
			if (!toggleImage) { return; }
			bool show = toggleMode && IsOn;
			if (toggleImage.enabled != show) { toggleImage.enabled = show; }
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

		[ContextMenu("Assign Refs")]
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
			if (toggleGroup != null) { toggleGroup.Register(this); }
			if (IsOn == value) { return; }
			if (!value && toggleGroup != null && !toggleGroup.CanTurnOff(this)) { return; }

			_ApplyIsOn(value);

			if (value && toggleGroup != null) { toggleGroup.NotifyToggledOn(this); }
		}

		/// <summary>Group-driven off/on without re-entering <see cref="ButtonXToggleGroup"/> notify.</summary>
		internal void SetIsOnFromGroup(bool value)
		{
			if (!toggleMode || IsOn == value) { return; }
			_ApplyIsOn(value);
		}

		private void _ApplyIsOn(bool value)
		{
			IsOn = value;
			if (IsOn) { OnToggledOn?.Invoke(); } else { OnToggledOff?.Invoke(); }
			_ApplyVisualState(IsOn ? ButtonVisualState.Selected : ButtonVisualState.Normal);
		}
	}
}
