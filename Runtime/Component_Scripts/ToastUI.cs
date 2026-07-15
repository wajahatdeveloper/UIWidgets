using System;
using System.Collections;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	public class ToastUI : PersistentSingletonBehaviour<ToastUI>
	{
		[Header("UI References :")]
		[SerializeField] private CanvasGroup uiCanvasGroup;
		[SerializeField] private VerticalLayoutGroup uiContentVerticalLayoutGroup;
		[SerializeField] private Image uiImage;
		[SerializeField] private TextMeshProUGUI uiText;
		[SerializeField] private ButtonX clickButton;
		[Tooltip("Optional. When assigned, WithIcon / severity icons can show here.")]
		[SerializeField] private Image uiIcon;

		[Header("Toast Colors :")]
		[Tooltip("Optional shared palette. When assigned, overrides the inline Colors array for ToastColor lookups.")]
		[SerializeField] private ToastColorPalette palette;
		[SerializeField] private Color[] colors;

		[Header("Toast Fade In/Out Duration :")]
		[Range(0.05f, 2f)]
		[SerializeField] private float fadeDuration = 0.3f;
		[Tooltip("Use unscaled time for UI fade and wait.")]
		[SerializeField] private bool useUnscaledTime = true;

		[Header("Positioning :")]
		[SerializeField] private float positionOffset = 20f;

		[Tooltip("Longer text is truncated with an ellipsis.")]
		[SerializeField] private int maxTextLength = 300;

		[Header("Queue :")]
		[SerializeField] private int maxQueueSize = 8;

		[Header("Contrast :")]
		[SerializeField] private Color lightTextColor = Color.white;
		[SerializeField] private Color darkTextColor = Color.black;

		private readonly Queue<ToastRequest> _queue = new Queue<ToastRequest>();
		private ToastRequest _current;
		private bool _isShowing;
		private Coroutine _currentFadeCoroutine;
		private RectOffset _basePadding;

		protected override void Awake()
		{
			base.Awake();
			ValidateRefs();

			if (uiContentVerticalLayoutGroup != null)
			{
				_basePadding = new RectOffset(
					uiContentVerticalLayoutGroup.padding.left,
					uiContentVerticalLayoutGroup.padding.right,
					uiContentVerticalLayoutGroup.padding.top,
					uiContentVerticalLayoutGroup.padding.bottom);
			}

			uiCanvasGroup.alpha = 0f;
			SetRaycasts(false);
			clickButton.OnClicked.AddListener(OnClickToDismiss);
			clickButton.Interactable = false;
			clickButton.clickCooldownSeconds = 0f;

			if (uiIcon != null)
			{
				uiIcon.enabled = false;
				uiIcon.gameObject.SetActive(false);
			}
		}

		protected override void OnDestroy()
		{
			if (clickButton != null)
			{
				clickButton.OnClicked.RemoveListener(OnClickToDismiss);
			}
			base.OnDestroy();
		}

		/// <summary>Enqueue a toast. Plays immediately when idle; otherwise waits in FIFO order.</summary>
		public void Enqueue(ToastRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			if (request.Replace)
			{
				ClearQueue();
				StopCurrent(invokeDismiss: true);
				_queue.Enqueue(request);
				TryPlayNext();
				return;
			}

			if (_queue.Count >= maxQueueSize)
			{
				throw new InvalidOperationException(
					$"[UI:ERROR] ToastUI queue is full (max {maxQueueSize}). Dismiss or raise maxQueueSize.");
			}

			_queue.Enqueue(request);
			TryPlayNext();
		}

		/// <summary>Legacy entry used by older Init overloads.</summary>
		public void Init(string text, float duration, ToastColor color, ToastPosition position)
		{
			Enqueue(ToastRequest.Create(text, duration, ResolveColor(color), position, false, null, null, false));
		}

		public void Init(string text, float duration, Color color, ToastPosition position)
		{
			Enqueue(ToastRequest.Create(text, duration, color, position, false, null, null, false));
		}

		public void Init(string text, float duration, ToastColor color, ToastPosition position, bool clickToDismiss, Action onDismiss)
		{
			Enqueue(ToastRequest.Create(text, duration, ResolveColor(color), position, clickToDismiss, onDismiss, null, false));
		}

		public void Init(string text, float duration, Color color, ToastPosition position, bool clickToDismiss, Action onDismiss)
		{
			Enqueue(ToastRequest.Create(text, duration, color, position, clickToDismiss, onDismiss, null, false));
		}

		public Color ResolveColor(ToastColor color)
		{
			int i = (int)color;
			if (palette != null)
			{
				if (i < 0 || i >= palette.Count)
				{
					throw new InvalidOperationException(
						$"[UI:ERROR] ToastUI palette missing color for {color} (index {i}, count {palette.Count}).");
				}
				return palette.GetColor(i);
			}

			if (colors == null || i < 0 || i >= colors.Length)
			{
				throw new InvalidOperationException(
					$"[UI:ERROR] ToastUI colors array missing entry for {color} (index {i}). Assign ToastColorPalette or colors[].");
			}
			return colors[i];
		}

		public void Dismiss()
		{
			StopCurrent(invokeDismiss: true);
			TryPlayNext();
		}

		public void DismissAll()
		{
			ClearQueue();
			StopCurrent(invokeDismiss: true);
		}

		public int QueuedCount => _queue.Count;
		public bool IsShowing => _isShowing;

		private void ValidateRefs()
		{
			if (uiCanvasGroup == null ||
			    uiContentVerticalLayoutGroup == null ||
			    uiImage == null ||
			    uiText == null ||
			    clickButton == null)
			{
				DebugX.Logger(LogChannels.UI).Error(
					"[UI:ERROR] ToastUI: required refs missing (CanvasGroup, VerticalLayoutGroup, Image, Text, ButtonX).");
				throw new InvalidOperationException(
					"[UI:ERROR] ToastUI: required serialized references are not assigned.");
			}
		}

		private void TryPlayNext()
		{
			if (_isShowing || _queue.Count == 0)
			{
				return;
			}

			_current = _queue.Dequeue();
			_isShowing = true;
			ApplyRequest(_current);
			_currentFadeCoroutine = StartCoroutine(FadeInOut(_current.Duration, fadeDuration));
		}

		private void ApplyRequest(ToastRequest request)
		{
			string text = request.Text;
			if (!string.IsNullOrEmpty(text) && text.Length > maxTextLength)
			{
				text = text.Substring(0, maxTextLength) + "...";
			}

			uiText.text = text;
			clickButton.Text = text ?? string.Empty;

			Color bg = request.Color;
			SyncButtonTint(bg);
			uiText.color = ContrastTextColor(bg);

			SetupPosition(request.Position);
			ApplyIcon(request.Icon);

			bool clickToDismiss = request.ClickToDismiss;
			clickButton.Interactable = clickToDismiss;
			// Interactable re-applies ColorTint; keep fill exact.
			uiImage.color = bg;
			SetRaycasts(clickToDismiss);
		}

		private void ApplyIcon(Sprite icon)
		{
			if (uiIcon == null)
			{
				return;
			}

			bool show = icon != null;
			uiIcon.gameObject.SetActive(show);
			uiIcon.enabled = show;
			uiIcon.sprite = icon;
			uiIcon.raycastTarget = false;
		}

		private void SyncButtonTint(Color color)
		{
			clickButton.normalColor = color;
			clickButton.highlightColor = color;
			clickButton.pressedColor = new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f, color.a);
			clickButton.selectedColor = color;
			clickButton.disabledColor = color;
		}

		private Color ContrastTextColor(Color background)
		{
			float luminance = (0.299f * background.r) + (0.587f * background.g) + (0.114f * background.b);
			return luminance < 0.55f ? lightTextColor : darkTextColor;
		}

		private void SetupPosition(ToastPosition position)
		{
			uiContentVerticalLayoutGroup.childAlignment = (TextAnchor)((int)position);

			int o = Mathf.RoundToInt(positionOffset);
			var pad = new RectOffset(_basePadding.left, _basePadding.right, _basePadding.top, _basePadding.bottom);

			switch (position)
			{
				case ToastPosition.TopLeft:
				case ToastPosition.TopCenter:
				case ToastPosition.TopRight:
					pad.top = Mathf.Max(pad.top, o);
					break;
				case ToastPosition.BottomLeft:
				case ToastPosition.BottomCenter:
				case ToastPosition.BottomRight:
					pad.bottom = Mathf.Max(pad.bottom, o);
					break;
			}

			switch (position)
			{
				case ToastPosition.TopLeft:
				case ToastPosition.MiddleLeft:
				case ToastPosition.BottomLeft:
					pad.left = Mathf.Max(pad.left, o);
					break;
				case ToastPosition.TopRight:
				case ToastPosition.MiddleRight:
				case ToastPosition.BottomRight:
					pad.right = Mathf.Max(pad.right, o);
					break;
			}

			uiContentVerticalLayoutGroup.padding = pad;
		}

		private void SetRaycasts(bool enabled)
		{
			uiCanvasGroup.blocksRaycasts = enabled;
			uiCanvasGroup.interactable = enabled;
			if (uiImage != null)
			{
				uiImage.raycastTarget = enabled;
			}
		}

		private void OnClickToDismiss()
		{
			if (_current != null && _current.ClickToDismiss)
			{
				Dismiss();
			}
		}

		private IEnumerator FadeInOut(float toastDuration, float fadeSeconds)
		{
			yield return null;
			uiContentVerticalLayoutGroup.CalculateLayoutInputHorizontal();
			uiContentVerticalLayoutGroup.CalculateLayoutInputVertical();
			uiContentVerticalLayoutGroup.SetLayoutHorizontal();
			uiContentVerticalLayoutGroup.SetLayoutVertical();
			yield return null;

			yield return Fade(uiCanvasGroup, 0f, 1f, fadeSeconds);
			if (useUnscaledTime)
			{
				yield return new WaitForSecondsRealtime(toastDuration);
			}
			else
			{
				yield return new WaitForSeconds(toastDuration);
			}
			yield return Fade(uiCanvasGroup, 1f, 0f, fadeSeconds);

			FinishCurrentAndContinue();
		}

		private void FinishCurrentAndContinue()
		{
			_currentFadeCoroutine = null;
			InvokeDismissOnce(_current);
			_current = null;
			_isShowing = false;
			SetRaycasts(false);
			if (clickButton != null)
			{
				clickButton.Interactable = false;
			}
			TryPlayNext();
		}

		private void StopCurrent(bool invokeDismiss)
		{
			if (_currentFadeCoroutine != null)
			{
				StopCoroutine(_currentFadeCoroutine);
				_currentFadeCoroutine = null;
			}

			uiCanvasGroup.alpha = 0f;
			SetRaycasts(false);
			if (clickButton != null)
			{
				clickButton.Interactable = false;
			}

			if (_isShowing)
			{
				if (invokeDismiss)
				{
					InvokeDismissOnce(_current);
				}
				_current = null;
				_isShowing = false;
			}
		}

		private void ClearQueue()
		{
			_queue.Clear();
		}

		private static void InvokeDismissOnce(ToastRequest request)
		{
			if (request == null)
			{
				return;
			}

			var callback = request.OnDismiss;
			request.OnDismiss = null;
			callback?.Invoke();
		}

		private IEnumerator Fade(CanvasGroup cGroup, float startAlpha, float endAlpha, float duration)
		{
			if (cGroup == null)
			{
				DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] ToastUI: Fade called with null CanvasGroup.");
				yield break;
			}

			if (duration <= 0f)
			{
				cGroup.alpha = endAlpha;
				yield break;
			}

			float startTime = useUnscaledTime ? Time.unscaledTime : Time.time;
			float t = 0f;
			while (t < 1f)
			{
				float now = useUnscaledTime ? Time.unscaledTime : Time.time;
				t = (now - startTime) / duration;
				if (t > 1f)
				{
					t = 1f;
				}
				cGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
				yield return null;
			}

			cGroup.alpha = endAlpha;
		}
	}

	public sealed class ToastRequest
	{
		public string Text;
		public float Duration;
		public Color Color;
		public ToastPosition Position;
		public bool ClickToDismiss;
		public Action OnDismiss;
		public Sprite Icon;
		public bool Replace;

		public static ToastRequest Create(
			string text,
			float duration,
			Color color,
			ToastPosition position,
			bool clickToDismiss,
			Action onDismiss,
			Sprite icon,
			bool replace)
		{
			return new ToastRequest
			{
				Text = text,
				Duration = duration,
				Color = color,
				Position = position,
				ClickToDismiss = clickToDismiss,
				OnDismiss = onDismiss,
				Icon = icon,
				Replace = replace
			};
		}
	}
}
