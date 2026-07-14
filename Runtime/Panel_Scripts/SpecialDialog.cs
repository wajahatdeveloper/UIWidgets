using System;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	public class SpecialDialog : Dialog
	{
		/// <summary>
		/// Entry point to create or refresh custom layout. Override in derived dialogs.
		/// Called right before the dialog is shown.
		/// </summary>
		protected virtual void BuildLayout()
		{
			// Implement in subclasses. For example: populate customContentRoot with dynamic elements.
		}

		/// <summary>
		/// Hook called before showing. Override for setup.
		/// </summary>
		protected virtual void OnBeforeShow() { }

		/// <summary>
		/// Hook called after showing. Override for animations/focus.
		/// </summary>
		protected virtual void OnAfterShow() { }

		/// <summary>
		/// Hook called before hiding. Override for teardown.
		/// </summary>
		protected virtual void OnBeforeHide() { }

		/// <summary>
		/// Hook called after hiding. Override for cleanup.
		/// </summary>
		protected virtual void OnAfterHide() { }

		/// <summary>
		/// Show with the base button mapping, plus custom layout.
		/// </summary>
		public override void Show(string text, string titleText = "", Action onYes = null, Action onNo = null, Action onOk = null, Action onCancel = null, Action onClose = null, DialogIcon iconType = DialogIcon.None, Sprite customIconSprite = null)
		{
			OnBeforeShow();
			BuildLayout();
			base.Show(text, titleText, onYes, onNo, onOk, onCancel, onClose, iconType, customIconSprite);
			OnAfterShow();
		}

		/// <summary>
		/// Show using a base Layout mapping, with custom layout support.
		/// </summary>
		public override void Show(string text, string titleText, Dialog.Layout layout, Action onPrimary = null, Action onSecondary = null, Action onTertiary = null, Action onClose = null, DialogIcon iconType = DialogIcon.None, Sprite customIconSprite = null)
		{
			OnBeforeShow();
			BuildLayout();
			base.Show(text, titleText, layout, onPrimary, onSecondary, onTertiary, onClose, iconType, customIconSprite);
			OnAfterShow();
		}

		public override void Hide()
		{
			OnBeforeHide();
			base.Hide();
			OnAfterHide();
		}

		/// <summary>
		/// Utility to wire an extra button to an action and toggle its visibility.
		/// </summary>
		protected void RegisterExtraButton(Button button, Action onClick)
		{
			if (button == null) return;
			button.onClick.RemoveAllListeners();
			if (onClick != null)
			{
				button.onClick.AddListener(() =>
				{
					onClick.Invoke();
					Hide();
				});
			}
			button.gameObject.SetActive(onClick != null);
		}

		protected void RegisterExtraButton(ButtonX button, Action onClick)
		{
			if (button == null) return;
			button.OnClicked.RemoveAllListeners();
			if (onClick != null)
			{
				button.OnClicked.AddListener(() =>
				{
					onClick.Invoke();
					Hide();
				});
			}
			button.gameObject.SetActive(onClick != null);
		}
	}
}


