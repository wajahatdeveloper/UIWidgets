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
	[SerializeField] bool canvasDragDropEnabled = true;
	[SerializeField] bool dragDropSelectsCreated = true;
	[SerializeField] bool dragDropSetNativeSize = true;
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

		/// <summary>Drop a Sprite/Texture/UI-prefab from the Project window onto a Canvas in the Scene View.</summary>
		public bool CanvasDragDropEnabled
		{
			get => canvasDragDropEnabled;
			set => Set(ref canvasDragDropEnabled, value);
		}

		public bool DragDropSelectsCreated
		{
			get => dragDropSelectsCreated;
			set => Set(ref dragDropSelectsCreated, value);
		}

		/// <summary>Apply SetNativeSize to created Image/RawImage elements.</summary>
		public bool DragDropSetNativeSize
		{
			get => dragDropSetNativeSize;
			set => Set(ref dragDropSetNativeSize, value);
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
