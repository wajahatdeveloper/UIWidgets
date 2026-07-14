using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIWidgets.Editor
{
	public static class UIWidgetsMenuItems
	{
		private static UIWidgetsAssetScriptable _cachedAsset;
		private static Dictionary<string, UIWidget> _widgetLookup;

		private static bool ShouldInstantiateAsPrefab()
		{
			return UIWidgets.GetIsInstantiatingPrefab();
		}

		private static GameObject InstantiateWidget(GameObject prefab, Transform parent = null)
		{
			if (prefab == null) return null;
			if (ShouldInstantiateAsPrefab())
				return parent != null
					? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject
					: PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			return parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
		}

		#region Asset Loading and Caching

		private static bool TryLoadUIWidgetsAsset()
		{
			if (_cachedAsset != null)
			{
				return true;
			}

			var guids = AssetDatabase.FindAssets("t:UIWidgetsAssetScriptable");
			if (guids == null || guids.Length == 0)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgetsAssetScriptable not found in project.");
				return false;
			}

			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			_cachedAsset = AssetDatabase.LoadAssetAtPath<UIWidgetsAssetScriptable>(path);

			if (_cachedAsset == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] Failed to load UIWidgetsAssetScriptable from {Path}", path);
				return false;
			}

			// Rebuild lookup dictionary
			_widgetLookup = new Dictionary<string, UIWidget>(StringComparer.OrdinalIgnoreCase);
			if (_cachedAsset.widgets != null)
			{
				foreach (var widget in _cachedAsset.widgets)
				{
					if (!string.IsNullOrEmpty(widget.widgetName))
					{
						_widgetLookup[widget.widgetName] = widget;
					}
				}
			}

			return true;
		}

		private static void InvalidateCache()
		{
			_cachedAsset = null;
			_widgetLookup = null;
		}

		/// <summary>
		/// Invalidates the cached UIWidgetsAssetScriptable lookup whenever any asset is
		/// imported, deleted, or moved, so edits to the asset (added/renamed widgets) are
		/// picked up without requiring a domain reload.
		/// </summary>
		private class UIWidgetsAssetChangeWatcher : AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(
				string[] importedAssets,
				string[] deletedAssets,
				string[] movedAssets,
				string[] movedFromAssetPaths)
			{
				InvalidateCache();
			}
		}

		#endregion

		#region Helper Methods

		private static Transform GetOrCreateCanvas(Transform preferredParent = null)
		{
			// Check for existing Canvas
			var existingCanvas = Object.FindFirstObjectByType<Canvas>();
			if (existingCanvas != null)
			{
				return existingCanvas.transform;
			}

			// Try to use Canvas widget from asset
			if (_cachedAsset != null && _widgetLookup != null)
			{
				if (_widgetLookup.TryGetValue("Canvas", out var canvasWidget) && canvasWidget.widgetPrefab != null)
				{
					var canvasPrefab = canvasWidget.widgetPrefab;
					GameObject newCanvas = InstantiateWidget(canvasPrefab, preferredParent);
					Undo.RegisterCreatedObjectUndo(newCanvas, "Create new Canvas");
					return newCanvas.transform;
				}
			}

			// Create standard Canvas
			GameObject canvasGO = new GameObject("Canvas");
			canvasGO.layer = LayerMask.NameToLayer("UI");
			Canvas canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();

			if (preferredParent != null)
			{
				canvasGO.transform.SetParent(preferredParent);
			}

			Undo.RegisterCreatedObjectUndo(canvasGO, "Create new Canvas");
			return canvasGO.transform;
		}

		private static void EnsureEventSystemExists()
		{
			var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
			if (existingEventSystem != null)
			{
				return;
			}

			// Try to use EventSystem widget from asset
			if (_cachedAsset != null && _widgetLookup != null)
			{
				if (_widgetLookup.TryGetValue("Utility", out var utilityWidget) && utilityWidget.widgetVariations != null)
				{
					var eventSystemVariation = utilityWidget.widgetVariations.FirstOrDefault(v => v.widgetName == "EventSystem");
					if (eventSystemVariation != null && eventSystemVariation.widgetPrefab != null)
					{
						GameObject newEventSystem = InstantiateWidget(eventSystemVariation.widgetPrefab);
						Undo.RegisterCreatedObjectUndo(newEventSystem, "Create new EventSystem");
						return;
					}
				}
			}

			// Create standard EventSystem
			GameObject es = new GameObject("EventSystem");
			es.AddComponent<EventSystem>();
			es.AddComponent<StandaloneInputModule>();
			Undo.RegisterCreatedObjectUndo(es, "Create new EventSystem");
		}

		private static void CreateWidgetFromAsset(string widgetName, MenuCommand menuCommand, GameObject variationPrefab = null)
		{
			// Load asset if needed
			if (!TryLoadUIWidgetsAsset())
			{
				return;
			}

			// Find widget
			if (!_widgetLookup.TryGetValue(widgetName, out var widget))
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] Widget '{WidgetName}' not found in UIWidgetsAsset.", widgetName);
				return;
			}

			// Determine prefab to use
			GameObject prefabToInstantiate = variationPrefab ?? widget.widgetPrefab;
			if (prefabToInstantiate == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] Prefab for widget '{WidgetName}' is null.", widgetName);
				return;
			}

			// Determine parent
			Transform parent = null;

			// Check MenuCommand context first
			if (menuCommand.context is GameObject contextGO)
			{
				var rectTransform = contextGO.GetComponent<RectTransform>();
				if (rectTransform != null)
				{
					parent = rectTransform;
				}
			}

			// Check Selection if no context
			if (parent == null)
			{
				var selectedTransform = Selection.activeTransform;
				if (selectedTransform != null)
				{
					var rectTransform = selectedTransform.GetComponent<RectTransform>();
					if (rectTransform != null)
					{
						parent = rectTransform;
					}
				}
			}

			// Get or create Canvas if needed
			if (parent == null && !widget.noCanvasRequired)
			{
				parent = GetOrCreateCanvas();
			}

			// Ensure EventSystem exists
			EnsureEventSystemExists();

			// Instantiate prefab
			GameObject itemObject;
			itemObject = InstantiateWidget(prefabToInstantiate, parent);

			if (itemObject == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] Failed to instantiate prefab for widget '{WidgetName}'.", widgetName);
				return;
			}

			// Register Undo
			Undo.RegisterCreatedObjectUndo(itemObject, $"Create {widgetName}");

			// Remove clone suffix
			itemObject.name = itemObject.name.RemoveCloneSuffix();

			// Select the new GameObject
			Selection.activeGameObject = itemObject;
		}

		/// <summary>Create from a prefab when widget has no lookup key (e.g. Screens with empty widgetName).</summary>
		private static void CreateFromPrefab(GameObject prefab, bool noCanvasRequired, MenuCommand menuCommand, string undoLabel)
		{
			if (!TryLoadUIWidgetsAsset() || prefab == null) return;

			Transform parent = null;
			if (menuCommand.context is GameObject contextGO && contextGO.GetComponent<RectTransform>() != null)
				parent = contextGO.transform;
			if (parent == null && Selection.activeTransform != null && Selection.activeTransform.GetComponent<RectTransform>() != null)
				parent = Selection.activeTransform;

			if (parent == null && !noCanvasRequired)
				parent = GetOrCreateCanvas();
			EnsureEventSystemExists();

			GameObject itemObject = InstantiateWidget(prefab, parent);
			if (itemObject == null) return;

			Undo.RegisterCreatedObjectUndo(itemObject, undoLabel);
			itemObject.name = itemObject.name.RemoveCloneSuffix();
			Selection.activeGameObject = itemObject;
		}

		private static void CreateScreenVariation(string variationName, MenuCommand menuCommand)
		{
			if (!TryLoadUIWidgetsAsset() || _cachedAsset?.widgets == null) return;
			var screensWidget = _cachedAsset.widgets.FirstOrDefault(w => w.category == "Screens" && string.IsNullOrEmpty(w.widgetName));
			var variation = screensWidget?.widgetVariations?.FirstOrDefault(v => v.widgetName == variationName);
			if (variation?.widgetPrefab == null) return;
			CreateFromPrefab(variation.widgetPrefab, variation.noCanvasRequired, menuCommand, $"Create {variationName}");
		}

		private static void CreateVariationHandler(string widgetName, string variationName, MenuCommand menuCommand)
		{
			if (TryLoadUIWidgetsAsset() && _widgetLookup.TryGetValue(widgetName, out var widget))
			{
				var variation = widget.widgetVariations?.FirstOrDefault(v => v.widgetName == variationName);
				if (variation != null && variation.widgetPrefab != null)
					CreateWidgetFromAsset(widgetName, menuCommand, variation.widgetPrefab);
			}
		}

		#endregion

		#region Menu Items - GameObject/UIWidgets (top-level, before UI)

		const string MenuRoot = "GameObject/UIWidgets/";

		// ----- Containers (10) -----
		[MenuItem(MenuRoot + "Containers/Panel", false, 10)]
		private static void CreatePanel(MenuCommand menuCommand) => CreateWidgetFromAsset("Panel", menuCommand);

		[MenuItem(MenuRoot + "Containers/Canvas", false, 11)]
		private static void CreateCanvas(MenuCommand menuCommand) => CreateWidgetFromAsset("Canvas", menuCommand);

		[MenuItem(MenuRoot + "Containers/ScollableList", false, 12)]
		private static void CreateScollableList(MenuCommand menuCommand) => CreateWidgetFromAsset("ScollableList", menuCommand);

		[MenuItem(MenuRoot + "Containers/Tabs", false, 13)]
		private static void CreateTabs(MenuCommand menuCommand) => CreateWidgetFromAsset("Tabs", menuCommand);

		// ----- Buttons (20): ButtonX submenu (no duplicate - default under submenu) -----
		[MenuItem(MenuRoot + "Buttons/ButtonX/ButtonX", false, 20)]
		private static void CreateButtonXDefault(MenuCommand menuCommand) => CreateWidgetFromAsset("ButtonX", menuCommand);

		[MenuItem(MenuRoot + "Buttons/ButtonX/ButtonTMP", false, 21)]
		private static void CreateButtonTMP(MenuCommand menuCommand) => CreateVariationHandler("ButtonX", "ButtonTMP", menuCommand);

		[MenuItem(MenuRoot + "Buttons/Toggle/Toggle", false, 23)]
		private static void CreateToggle(MenuCommand menuCommand) => CreateWidgetFromAsset("Toggle", menuCommand);

		[MenuItem(MenuRoot + "Buttons/Toggle/Toggle Image", false, 24)]
		private static void CreateToggleImage(MenuCommand menuCommand) => CreateVariationHandler("Toggle", "Toggle Image", menuCommand);

		[MenuItem(MenuRoot + "Buttons/Stepper", false, 25)]
		private static void CreateStepper(MenuCommand menuCommand) => CreateWidgetFromAsset("Stepper", menuCommand);

		// ----- DropDown (30) -----
		[MenuItem(MenuRoot + "DropDown/DropDown", false, 30)]
		private static void CreateDropDown(MenuCommand menuCommand) => CreateWidgetFromAsset("DropDown", menuCommand);

		// ----- Image (40) -----
		[MenuItem(MenuRoot + "Image/Image", false, 40)]
		private static void CreateImage(MenuCommand menuCommand) => CreateWidgetFromAsset("Image", menuCommand);

		[MenuItem(MenuRoot + "Image/RawImage", false, 41)]
		private static void CreateRawImage(MenuCommand menuCommand) => CreateVariationHandler("Image", "RawImage", menuCommand);

		// ----- InputField (50) -----
		[MenuItem(MenuRoot + "InputField/InputField", false, 50)]
		private static void CreateInputField(MenuCommand menuCommand) => CreateWidgetFromAsset("InputField", menuCommand);

		// ----- Slider (60) - asset category is "Slider" -----
		[MenuItem(MenuRoot + "Slider/Slider", false, 60)]
		private static void CreateSlider(MenuCommand menuCommand) => CreateWidgetFromAsset("Slider", menuCommand);

		[MenuItem(MenuRoot + "Slider/BoxSlider", false, 61)]
		private static void CreateBoxSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "BoxSlider", menuCommand);

		[MenuItem(MenuRoot + "Slider/MinMaxSlider", false, 62)]
		private static void CreateMinMaxSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "MinMaxSlider", menuCommand);

		[MenuItem(MenuRoot + "Slider/RadialSlider", false, 63)]
		private static void CreateRadialSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "RadialSlider", menuCommand);

		[MenuItem(MenuRoot + "Slider/RangeSlider", false, 64)]
		private static void CreateRangeSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "RangeSlider", menuCommand);

		// ----- Text (70) -----
		[MenuItem(MenuRoot + "Text/Text", false, 70)]
		private static void CreateText(MenuCommand menuCommand) => CreateWidgetFromAsset("Text", menuCommand);

		// ----- Containers: Cards (75) - asset has category Containers, widgetName Cards, variations only -----
		[MenuItem(MenuRoot + "Containers/Cards/CardExpanding", false, 75)]
		private static void CreateCardExpanding(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardExpanding", menuCommand);

		[MenuItem(MenuRoot + "Containers/Cards/CardPopup", false, 76)]
		private static void CreateCardPopup(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardPopup", menuCommand);

		[MenuItem(MenuRoot + "Containers/Cards/CardStack", false, 77)]
		private static void CreateCardStack(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardStack", menuCommand);

		// ----- Utility (80) -----
		[MenuItem(MenuRoot + "Utility/Tooltip/Tooltip", false, 80)]
		private static void CreateTooltip(MenuCommand menuCommand) => CreateWidgetFromAsset("Tooltip", menuCommand);

		[MenuItem(MenuRoot + "Utility/Tooltip/TooltipTrigger", false, 81)]
		private static void CreateTooltipTrigger(MenuCommand menuCommand) => CreateVariationHandler("Tooltip", "TooltipTrigger", menuCommand);

		[MenuItem(MenuRoot + "Utility/Setup Default State", false, 82)]
		private static void CreateSetupDefaultState(MenuCommand menuCommand) => CreateVariationHandler("Utility", "Setup Default State", menuCommand);

		[MenuItem(MenuRoot + "Utility/EventSystem", false, 83)]
		private static void CreateEventSystemWidget(MenuCommand menuCommand) => CreateVariationHandler("Utility", "EventSystem", menuCommand);

		// ----- Screens (90) - variation-only in asset -----
		[MenuItem(MenuRoot + "Screens/Dialog Screen", false, 90)]
		private static void CreateDialogScreen(MenuCommand menuCommand) => CreateScreenVariation("Dialog Screen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Fader Sceen", false, 91)]
		private static void CreateFaderScreen(MenuCommand menuCommand) => CreateScreenVariation("Fader Sceen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Input Dialog Screen", false, 92)]
		private static void CreateInputDialogScreen(MenuCommand menuCommand) => CreateScreenVariation("Input Dialog Screen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Loading Screen", false, 93)]
		private static void CreateLoadingScreen(MenuCommand menuCommand) => CreateScreenVariation("Loading Screen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Wait Screen", false, 94)]
		private static void CreateWaitScreen(MenuCommand menuCommand) => CreateScreenVariation("Wait Screen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Line Message Screen", false, 95)]
		private static void CreateLineMessageScreen(MenuCommand menuCommand) => CreateScreenVariation("Line Message Screen", menuCommand);

		[MenuItem(MenuRoot + "Screens/Toast Message Canvas", false, 96)]
		private static void CreateToastMessageCanvas(MenuCommand menuCommand) => CreateScreenVariation("Toast Message Canvas", menuCommand);

		#endregion
	}
}
