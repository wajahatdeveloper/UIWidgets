using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
	/// <summary>
	/// Project-wide UIWidgets editor settings, stored in ProjectSettings/UIWidgetsSettings.asset
	/// (version-controlled, shared by the team). Edited via Project Settings &gt; UIWidgets.
	/// </summary>
	[FilePath("ProjectSettings/UIWidgetsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	public class UIWidgetsSettings : ScriptableSingleton<UIWidgetsSettings>
	{
		public enum PickerFilter
		{
			All = 0,
			UI = 1,
			TwoD = 2,
			ThreeD = 3,
		}

		public enum PickerMouseButton
		{
			Right = 1,
			Middle = 2,
		}

		[SerializeField] bool scenePickerEnabled = true;
		[SerializeField] PickerMouseButton pickerMouseButton = PickerMouseButton.Right;
		[SerializeField] EventModifiers pickerModifiers = EventModifiers.None;
		[SerializeField] float pickerDragThreshold = 6f;
		[SerializeField] Color pickerOutlineColor = new Color(1f, 0.6f, 0f, 1f);
		[SerializeField] PickerFilter pickerDefaultFilter = PickerFilter.All;
		[SerializeField] bool pickerHoverPingsHierarchy = true;

		public bool ScenePickerEnabled
		{
			get => scenePickerEnabled;
			set => Set(ref scenePickerEnabled, value);
		}

		public PickerMouseButton MouseButton
		{
			get => pickerMouseButton;
			set => Set(ref pickerMouseButton, value);
		}

		/// <summary>Modifiers that must be held for the picker click (Shift/Control/Alt).
		/// Alt combos coexist with the SceneView camera: picking requires click-without-drag.</summary>
		public EventModifiers Modifiers
		{
			get => pickerModifiers;
			set => Set(ref pickerModifiers, value);
		}

		public float DragThreshold
		{
			get => pickerDragThreshold;
			set => Set(ref pickerDragThreshold, value);
		}

		public Color OutlineColor
		{
			get => pickerOutlineColor;
			set => Set(ref pickerOutlineColor, value);
		}

		public PickerFilter DefaultFilter
		{
			get => pickerDefaultFilter;
			set => Set(ref pickerDefaultFilter, value);
		}

		/// <summary>Hovering a picker row pings the object in the Hierarchy window.</summary>
		public bool HoverPingsHierarchy
		{
			get => pickerHoverPingsHierarchy;
			set => Set(ref pickerHoverPingsHierarchy, value);
		}

		void Set<T>(ref T field, T value)
		{
			if (!Equals(field, value))
			{
				field = value;
				Save(true);
			}
		}
	}
}
