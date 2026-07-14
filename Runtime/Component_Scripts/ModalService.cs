using System;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Minimal modal-panel coordinator. Exactly one modal is visible at a time; showing a new
	/// modal replaces the current one. A runtime-generated, input-blocking backdrop sits behind
	/// the active modal, and the modal is sorted above base UI via an in-place Canvas override.
	///
	/// Deliberately has NO stack, history, or back navigation — games here are built around
	/// standalone modal panels. Scene-scoped (not persistent): place one per UI scene.
	/// </summary>
	public sealed class ModalService : SingletonBehaviour<ModalService>
	{
		[Header("Backdrop")]
		[Tooltip("Color of the full-screen backdrop raised behind the active modal.")]
		[SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.6f);

		[Tooltip("When true, clicking the backdrop closes the current modal.")]
		[SerializeField] private bool closeOnBackdropClick = false;

		[Header("Sorting")]
		[Tooltip("Sorting order of the backdrop canvas. Must sit above base UI.")]
		[SerializeField] private int backdropSortingOrder = 1000;

		[Tooltip("Sorting order applied to the active modal panel (above the backdrop).")]
		[SerializeField] private int modalContentSortingOrder = 1001;

		/// <summary>The currently visible modal, or null when nothing is open.</summary>
		public PanelBase Current { get; private set; }

		/// <summary>True while a modal is open.</summary>
		public bool IsAnyOpen => Current != null;

		/// <summary>Raised whenever the active modal changes. Argument is the new current (null on close).</summary>
		public event Action<PanelBase> OnModalChanged;

		private GameObject _modalRoot;

		/// <summary>
		/// Shows <paramref name="panel"/> as the modal, replacing any currently open modal.
		/// </summary>
		public void Show(PanelBase panel)
		{
			if (panel == null)
			{
				DebugX.Builder(LogChannels.UI).WithContext(gameObject).Warning("ModalService.Show called with null panel.");
				return;
			}

			if (Current == panel && panel.IsShown())
			{
				return;
			}

			if (Current != null && Current != panel)
			{
				Current.HideInternal();
				ClearSortOrder(Current);
			}

			EnsureRoot();
			_modalRoot.SetActive(true);

			ApplySortOrder(panel);
			panel.ShowInternal();

			Current = panel;
			OnModalChanged?.Invoke(Current);
		}

		/// <summary>Hides the current modal and lowers the backdrop. No-op if nothing is open.</summary>
		public void HideCurrent()
		{
			if (Current == null)
			{
				return;
			}

			var panel = Current;
			Current = null;

			panel.HideInternal();
			ClearSortOrder(panel);

			if (_modalRoot != null)
			{
				_modalRoot.SetActive(false);
			}

			OnModalChanged?.Invoke(null);
		}

		private void EnsureRoot()
		{
			if (_modalRoot != null)
			{
				return;
			}

			_modalRoot = new GameObject("[ModalRoot]");
			_modalRoot.transform.SetParent(transform, false);

			var canvas = _modalRoot.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = true;
			canvas.sortingOrder = backdropSortingOrder;

			_modalRoot.AddComponent<CanvasScaler>();
			_modalRoot.AddComponent<GraphicRaycaster>();

			var backdrop = new GameObject("[Backdrop]");
			backdrop.transform.SetParent(_modalRoot.transform, false);

			var rect = backdrop.AddComponent<RectTransform>();
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;

			var image = backdrop.AddComponent<Image>();
			image.color = backdropColor;
			image.raycastTarget = true; // eats clicks so base UI behind the modal is not interactable

			if (closeOnBackdropClick)
			{
				var button = backdrop.AddComponent<Button>();
				button.transition = Selectable.Transition.None;
				button.onClick.AddListener(HideCurrent);
			}

			_modalRoot.SetActive(false);
		}

		private void ApplySortOrder(PanelBase panel)
		{
			var canvas = panel.GetComponent<Canvas>();
			if (canvas == null)
			{
				canvas = panel.gameObject.AddComponent<Canvas>();
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = modalContentSortingOrder;

			// A Canvas alone does not receive input; ensure the subtree can be clicked.
			if (panel.GetComponent<GraphicRaycaster>() == null)
			{
				panel.gameObject.AddComponent<GraphicRaycaster>();
			}
		}

		private void ClearSortOrder(PanelBase panel)
		{
			if (panel == null)
			{
				return;
			}
			var canvas = panel.GetComponent<Canvas>();
			if (canvas != null)
			{
				canvas.overrideSorting = false;
			}
		}
	}
}
