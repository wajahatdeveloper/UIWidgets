using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Click (without drag) in the SceneView opens a popup listing every object under the
	/// cursor, front to back. Input binding, outline color and drag threshold come from
	/// </summary>
	[InitializeOnLoad]
	public static class ScenePickerTrigger
	{
		const string MenuPath = AetherNexus.FoundationPlatform.Utilities.Menus.MenuPaths.UIWidgetsTools.ScenePickerEnabled;
		const EventModifiers RelevantModifiers = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt;

		static Vector2 downPosition;
		static bool tracking;
		static GameObject hovered;
		static readonly List<GameObject> pickBuffer = new List<GameObject>();
		static readonly Vector3[] rectCorners = new Vector3[4];

		/// <summary>Object highlighted in the SceneView while its row is hovered in the popup.</summary>
		public static GameObject Hovered
		{
			get => hovered;
			set
			{
				if (hovered == value)
				{
					return;
				}

				hovered = value;
				SceneView.RepaintAll();
			}
		}

		static ScenePickerTrigger()
		{
			SceneView.beforeSceneGui += OnBeforeSceneGui;
			SceneView.duringSceneGui += OnDuringSceneGui;
		}

		[MenuItem(MenuPath, false, 900)]
		static void ToggleEnabled() => UIWidgetsSettings.instance.ScenePickerEnabled = !UIWidgetsSettings.instance.ScenePickerEnabled;

		[MenuItem(MenuPath, true)]
		static bool ToggleEnabledValidate()
		{
			Menu.SetChecked(MenuPath, UIWidgetsSettings.instance.ScenePickerEnabled);
			return true;
		}

		[MenuItem(AetherNexus.FoundationPlatform.Utilities.Menus.MenuPaths.UIWidgetsTools.Settings, false, 902)]
		static void OpenSettings() => SettingsService.OpenProjectSettings("Project/UIWidgets");

		static bool InputMatches(Event evt)
		{
			var settings = UIWidgetsSettings.instance;
			return evt.button == (int)settings.MouseButton
				&& (evt.modifiers & RelevantModifiers) == settings.Modifiers;
		}

		static void OnBeforeSceneGui(SceneView sceneView)
		{
			var settings = UIWidgetsSettings.instance;
			if (!settings.ScenePickerEnabled)
			{
				return;
			}

			var evt = Event.current;
			if (!InputMatches(evt))
			{
				return;
			}

			if (evt.type == EventType.MouseDown)
			{
				downPosition = evt.mousePosition;
				tracking = true;
			}
			else if (evt.type == EventType.MouseUp && tracking)
			{
				tracking = false;
				if (Vector2.Distance(downPosition, evt.mousePosition) > settings.DragThreshold)
				{
					return;
				}

				var picked = PickAll(evt.mousePosition);
				if (picked.Count == 0)
				{
					return;
				}

				GUIUtility.hotControl = 0;
				var screenPoint = GUIUtility.GUIToScreenPoint(evt.mousePosition);
				evt.Use();
				ScenePickerWindow.Open(screenPoint, picked);
			}
		}

		/// <summary>
		/// Unity has no "pick everything at position" API; PickGameObject cycles through the
		/// stack when already-picked objects are passed as the ignore list.
		/// </summary>
		static List<GameObject> PickAll(Vector2 guiPosition)
		{
			pickBuffer.Clear();
			while (true)
			{
				var go = HandleUtility.PickGameObject(guiPosition, selectPrefabRoot: false, ignore: pickBuffer.ToArray());
				if (go == null)
				{
					break;
				}

				pickBuffer.Add(go);
			}

			return pickBuffer;
		}

		static void OnDuringSceneGui(SceneView sceneView)
		{
			if (hovered == null)
			{
				return;
			}

			Handles.color = UIWidgetsSettings.instance.OutlineColor;

			if (hovered.transform is RectTransform rect)
			{
				rect.GetWorldCorners(rectCorners);
				Handles.DrawLine(rectCorners[0], rectCorners[1]);
				Handles.DrawLine(rectCorners[1], rectCorners[2]);
				Handles.DrawLine(rectCorners[2], rectCorners[3]);
				Handles.DrawLine(rectCorners[3], rectCorners[0]);
				return;
			}

			var renderers = hovered.GetComponentsInChildren<Renderer>();
			if (renderers.Length == 0)
			{
				return;
			}

			var bounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; i++)
			{
				bounds.Encapsulate(renderers[i].bounds);
			}

			Handles.DrawWireCube(bounds.center, bounds.size);
		}
	}
}
