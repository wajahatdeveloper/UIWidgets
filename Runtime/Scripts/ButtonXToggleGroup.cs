using System.Collections.Generic;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Mutual-exclusion group for <see cref="ButtonX"/> toggle mode (Unity <c>ToggleGroup</c> analogue).
	/// Assign the same group on each member via <see cref="ButtonX.toggleGroup"/>.
	/// </summary>
	[AddComponentMenu("UI/ButtonX Toggle Group")]
	[DisallowMultipleComponent]
	public class ButtonXToggleGroup : MonoBehaviour
	{
		[Tooltip("When false, the active button cannot be turned off by clicking it again (one must stay on).")]
		[SerializeField] private bool allowSwitchOff;

		private readonly List<ButtonX> _buttons = new List<ButtonX>();

		public bool AllowSwitchOff
		{
			get => allowSwitchOff;
			set => allowSwitchOff = value;
		}

		public void Register(ButtonX button)
		{
			if (button == null || _buttons.Contains(button)) { return; }
			_buttons.Add(button);
		}

		public void Unregister(ButtonX button)
		{
			if (button == null) { return; }
			_buttons.Remove(button);
		}

		/// <summary>True if <paramref name="button"/> may turn off without violating allowSwitchOff.</summary>
		public bool CanTurnOff(ButtonX button)
		{
			if (allowSwitchOff || button == null || !button.IsOn) { return true; }
			for (int i = 0; i < _buttons.Count; i++)
			{
				var other = _buttons[i];
				if (other != null && other != button && other.isActiveAndEnabled && other.IsOn)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Called when a member turns on — turns all other registered members off.</summary>
		public void NotifyToggledOn(ButtonX button)
		{
			if (button == null) { return; }
			for (int i = 0; i < _buttons.Count; i++)
			{
				var other = _buttons[i];
				if (other == null || other == button) { continue; }
				if (other.IsOn) { other.SetIsOnFromGroup(false); }
			}
		}

		public void SetAllOff()
		{
			if (!allowSwitchOff) { return; }
			for (int i = 0; i < _buttons.Count; i++)
			{
				var button = _buttons[i];
				if (button != null && button.IsOn) { button.SetIsOnFromGroup(false); }
			}
		}
	}
}
