using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIWidgets
{
	/// <summary>
	/// Wrapper class that allows Unity Inspector to accept both UnityEngine.UI.Button and UIWidgets.ButtonX types.
	/// Provides a unified API for common button operations.
	/// </summary>
	[System.Serializable]
	public class ButtonReference
	{
		[SerializeField] private Object buttonObject; // Can be Button or ButtonX

		/// <summary>
		/// Direct access to the underlying Button component (null if not a Button).
		/// </summary>
		public Button Button => buttonObject as Button;

		/// <summary>
		/// Direct access to the underlying ButtonX component (null if not a ButtonX).
		/// </summary>
		public ButtonX ButtonX => buttonObject as ButtonX;

		/// <summary>
		/// Returns true if the stored object is a UnityEngine.UI.Button.
		/// </summary>
		public bool IsButton => buttonObject is Button;

		/// <summary>
		/// Returns true if the stored object is a UIWidgets.ButtonX.
		/// </summary>
		public bool IsButtonX => buttonObject is ButtonX;

		/// <summary>
		/// Returns true if no button is assigned.
		/// </summary>
		public bool IsNull => buttonObject == null;

		/// <summary>
		/// Gets the onClick UnityEvent from either Button or ButtonX.
		/// </summary>
		public UnityEvent onClick
		{
			get
			{
			if (buttonObject == null)
			{
				DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject is null. Cannot access onClick.");
				return null;
			}

			if (buttonObject is Button btn)
			{
				return btn.onClick;
			}

			if (buttonObject is ButtonX btnX)
			{
				return btnX.OnClicked;
			}

			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject must be Button or ButtonX, got {TypeName}.", buttonObject.GetType().Name);
			return null;
			}
		}

		/// <summary>
		/// Gets or sets the interactable state of the button.
		/// </summary>
		public bool interactable
		{
			get
			{
			if (buttonObject == null)
			{
				DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject is null. Cannot access interactable.");
				return false;
			}

			if (buttonObject is Button btn)
			{
				return btn.interactable;
			}

			if (buttonObject is ButtonX btnX)
			{
				return btnX.Interactable;
			}

			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject must be Button or ButtonX, got {TypeName}.", buttonObject.GetType().Name);
			return false;
			}
			set
			{
			if (buttonObject == null)
			{
				DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject is null. Cannot set interactable.");
				return;
			}

			if (buttonObject is Button btn)
			{
				btn.interactable = value;
				return;
			}

			if (buttonObject is ButtonX btnX)
			{
				btnX.Interactable = value;
				return;
			}

			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Button] buttonObject must be Button or ButtonX, got {TypeName}.", buttonObject.GetType().Name);
			}
		}

		/// <summary>
		/// Adds a listener to the button's onClick event.
		/// </summary>
		public void AddListener(UnityAction action)
		{
		if (action == null)
		{
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Button] AddListener called with null action.");
			return;
		}

			var evt = onClick;
			if (evt != null)
			{
				evt.AddListener(action);
			}
		}

		/// <summary>
		/// Removes a listener from the button's onClick event.
		/// </summary>
		public void RemoveListener(UnityAction action)
		{
		if (action == null)
		{
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Button] RemoveListener called with null action.");
			return;
		}

			var evt = onClick;
			if (evt != null)
			{
				evt.RemoveListener(action);
			}
		}

		/// <summary>
		/// Removes all listeners from the button's onClick event.
		/// </summary>
		public void RemoveAllListeners()
		{
			var evt = onClick;
			if (evt != null)
			{
				evt.RemoveAllListeners();
			}
		}
	}
}

