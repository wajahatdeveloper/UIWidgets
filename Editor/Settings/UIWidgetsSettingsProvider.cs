using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
	/// <summary>
	/// Project Settings &gt; UIWidgets. Scene Picker input/appearance is project-wide
	/// (ProjectSettings asset); the UIWidgets Window workflow toggles are per-user
	/// (EditorPrefs) and surfaced here so all package settings live on one page.
	/// </summary>
	static class UIWidgetsSettingsProvider
	{
		static readonly string[] ModifierNames = { "Shift", "Control", "Alt" };

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider("Project/UIWidgets", SettingsScope.Project)
			{
				label = "UIWidgets",
				keywords = new[] { "picker", "selection", "context", "scene", "outline", "layout", "widgets" },
				guiHandler = _ => OnGui(),
			};
		}

		static void OnGui()
		{
			var settings = UIWidgetsSettings.instance;

			EditorGUIUtility.labelWidth = 220f;
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Scene Picker", EditorStyles.boldLabel);

			settings.ScenePickerEnabled = EditorGUILayout.Toggle("Enabled", settings.ScenePickerEnabled);

			using (new EditorGUI.DisabledScope(!settings.ScenePickerEnabled))
			{
				settings.MouseButton = (UIWidgetsSettings.PickerMouseButton)EditorGUILayout.EnumPopup(
					new GUIContent("Mouse Button", "Click (without drag) in the SceneView that opens the picker."),
					settings.MouseButton);

				settings.Modifiers = MaskToModifiers(EditorGUILayout.MaskField(
					new GUIContent("Modifier Keys", "Keys that must be held with the click."),
					ModifiersToMask(settings.Modifiers),
					ModifierNames));

				if ((settings.Modifiers & EventModifiers.Alt) != 0)
				{
					EditorGUILayout.HelpBox(
						"Alt combos also drive the SceneView camera (orbit/zoom). The picker only " +
						"opens on a click without drag, but expect the camera to move on drags.",
						MessageType.Info);
				}

				if (settings.MouseButton == UIWidgetsSettings.PickerMouseButton.Right
					&& settings.Modifiers == EventModifiers.None)
				{
					EditorGUILayout.HelpBox(
						"Plain right-click suppresses Unity's Scene view context menu. " +
						"Add a modifier key or switch to the middle mouse button to keep it.",
						MessageType.Info);
				}

				settings.DragThreshold = EditorGUILayout.Slider(
					new GUIContent("Drag Threshold (px)", "Mouse travel above this counts as a camera drag, not a pick."),
					settings.DragThreshold, 1f, 20f);

				settings.OutlineColor = EditorGUILayout.ColorField(
					new GUIContent("Hover Outline Color"), settings.OutlineColor);

				settings.DefaultFilter = (UIWidgetsSettings.PickerFilter)EditorGUILayout.EnumPopup(
					new GUIContent("Default Filter", "Filter tab the picker window starts on."),
					settings.DefaultFilter);

				settings.HoverPingsHierarchy = EditorGUILayout.Toggle(
					new GUIContent("Hover Highlights In Hierarchy", "Hovering a row pings the object in the Hierarchy window."),
					settings.HoverPingsHierarchy);
			}

			EditorGUILayout.Space(12f);
			EditorGUILayout.LabelField("Canvas Drag && Drop", EditorStyles.boldLabel);

			settings.CanvasDragDropEnabled = EditorGUILayout.Toggle(
				new GUIContent("Enabled", "Drop a Sprite (→ Image), Texture (→ RawImage) or UI prefab from the Project window onto a Canvas in the Scene View."),
				settings.CanvasDragDropEnabled);

			using (new EditorGUI.DisabledScope(!settings.CanvasDragDropEnabled))
			{
				settings.DragDropSelectsCreated = EditorGUILayout.Toggle(
					new GUIContent("Select Created Element"), settings.DragDropSelectsCreated);
				settings.DragDropSetNativeSize = EditorGUILayout.Toggle(
					new GUIContent("Set Native Size", "Size created Image/RawImage elements to their texture."),
					settings.DragDropSetNativeSize);
			}

			EditorGUILayout.Space(12f);
			EditorGUILayout.LabelField("UIWidgets Window (per-user)", EditorStyles.boldLabel);

			PrefToggle("UIWidgets.IsInstantiatingPrefab", "Instantiate As Prefab", true);
			PrefToggle("UIWidgets.AutoSelectNewItems", "Auto-Select New Items", true);
			PrefToggle("UIWidgets.PreferExistingCanvas", "Prefer Existing Canvas", false);
			PrefToggle("UIWidgets.UseAutoNaming", "Use Auto Naming", false);

			EditorGUIUtility.labelWidth = 0f;
		}

		static void PrefToggle(string key, string label, bool defaultValue)
		{
			var current = EditorPrefs.GetBool(key, defaultValue);
			var next = EditorGUILayout.Toggle(label, current);
			if (next != current)
			{
				EditorPrefs.SetBool(key, next);
			}
		}

		static int ModifiersToMask(EventModifiers modifiers)
		{
			var mask = 0;
			if ((modifiers & EventModifiers.Shift) != 0)
			{
				mask |= 1;
			}

			if ((modifiers & EventModifiers.Control) != 0)
			{
				mask |= 2;
			}

			if ((modifiers & EventModifiers.Alt) != 0)
			{
				mask |= 4;
			}

			return mask;
		}

		static EventModifiers MaskToModifiers(int mask)
		{
			var modifiers = EventModifiers.None;
			if ((mask & 1) != 0)
			{
				modifiers |= EventModifiers.Shift;
			}

			if ((mask & 2) != 0)
			{
				modifiers |= EventModifiers.Control;
			}

			if ((mask & 4) != 0)
			{
				modifiers |= EventModifiers.Alt;
			}

			return modifiers;
		}
	}
}
