using System;
using System.Collections.Generic;
using System.Linq;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AetherNexus.UIWidgets.Editor
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
			var existingCanvas = Object.FindFirstObjectByType<Canvas>();
			if (existingCanvas != null)
				return existingCanvas.transform;

			GameObject canvasGO = new GameObject("Canvas");
			canvasGO.layer = LayerMask.NameToLayer("UI");
			Canvas canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();

			if (preferredParent != null)
				canvasGO.transform.SetParent(preferredParent);

			Undo.RegisterCreatedObjectUndo(canvasGO, "Create new Canvas");
			return canvasGO.transform;
		}

		private static void EnsureEventSystemExists()
		{
			if (Object.FindFirstObjectByType<EventSystem>() != null)
				return;

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

		private static void CreateVariationHandler(string widgetName, string variationName, MenuCommand menuCommand)
		{
			if (!TryLoadUIWidgetsAsset() || !_widgetLookup.TryGetValue(widgetName, out var widget))
				return;

			var variation = widget.widgetVariations?.FirstOrDefault(v => v.widgetName == variationName);
			if (variation?.widgetPrefab == null)
				return;

			CreateFromPrefab(variation.widgetPrefab, variation.noCanvasRequired, menuCommand, $"Create {variationName}");
		}

		/// <summary>Create a Singletons-catalog variation (Dialog, Fader, ModalService, etc.).</summary>
		private static void CreateSingletonVariation(string variationName, MenuCommand menuCommand)
			=> CreateVariationHandler("Singletons", variationName, menuCommand);

		#endregion

		#region Menu Items - GameObject/UI (Canvas) (flat siblings of Unity stock)

		// Flat under Unity 6's UI (Canvas) menu — unique leaf names avoid clashing with stock items.
		// Priorities sit after typical Unity/TMP UI entries (~2000).
		const string MenuRoot = "GameObject/UI (Canvas)/";
		const int MenuPriority = 2100;

		[MenuItem(MenuRoot + "Panel Base", false, MenuPriority)]
		private static void CreatePanel(MenuCommand menuCommand) => CreateWidgetFromAsset("Panel", menuCommand);

		[MenuItem(MenuRoot + "ScrollList", false, MenuPriority + 1)]
		private static void CreateScrollList(MenuCommand menuCommand) => CreateWidgetFromAsset("ScrollList", menuCommand);

		[MenuItem(MenuRoot + "ScrollList/ScrollList_Horizontal", false, MenuPriority + 2)]
		private static void CreateScrollListHorizontal(MenuCommand menuCommand) => CreateVariationHandler("ScrollList", "ScrollList_Horizontal", menuCommand);

		[MenuItem(MenuRoot + "ScrollList/ScrollList_Vertical", false, MenuPriority + 3)]
		private static void CreateScrollListVertical(MenuCommand menuCommand) => CreateVariationHandler("ScrollList", "ScrollList_Vertical", menuCommand);

		[MenuItem(MenuRoot + "Tabs", false, MenuPriority + 4)]
		private static void CreateTabs(MenuCommand menuCommand) => CreateWidgetFromAsset("Tabs", menuCommand);

		[MenuItem(MenuRoot + "Cards/CardExpanding", false, MenuPriority + 5)]
		private static void CreateCardExpanding(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardExpanding", menuCommand);

		[MenuItem(MenuRoot + "Cards/CardPopup", false, MenuPriority + 6)]
		private static void CreateCardPopup(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardPopup", menuCommand);

		[MenuItem(MenuRoot + "Cards/CardStack", false, MenuPriority + 7)]
		private static void CreateCardStack(MenuCommand menuCommand) => CreateVariationHandler("Cards", "CardStack", menuCommand);

		[MenuItem(MenuRoot + "ButtonX", false, MenuPriority + 10)]
		private static void CreateButtonXDefault(MenuCommand menuCommand) => CreateWidgetFromAsset("ButtonX", menuCommand);

		[MenuItem(MenuRoot + "ButtonX Toggle Group", false, MenuPriority + 11)]
		private static void CreateButtonXToggleGroup(MenuCommand menuCommand)
		{
			Transform parent = null;
			if (menuCommand.context is GameObject contextGO && contextGO.GetComponent<RectTransform>() != null)
				parent = contextGO.transform;
			if (parent == null && Selection.activeTransform != null && Selection.activeTransform.GetComponent<RectTransform>() != null)
				parent = Selection.activeTransform;
			if (parent == null)
				parent = GetOrCreateCanvas();
			EnsureEventSystemExists();

			var go = new GameObject("ButtonXToggleGroup", typeof(RectTransform), typeof(ButtonXToggleGroup));
			go.layer = LayerMask.NameToLayer("UI");
			go.transform.SetParent(parent, false);
			var rt = go.GetComponent<RectTransform>();
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			Undo.RegisterCreatedObjectUndo(go, "Create ButtonX Toggle Group");
			Selection.activeGameObject = go;
		}

		[MenuItem(MenuRoot + "Stepper", false, MenuPriority + 12)]
		private static void CreateStepper(MenuCommand menuCommand) => CreateWidgetFromAsset("Stepper", menuCommand);

		[MenuItem(MenuRoot + "Sliders/BoxSlider", false, MenuPriority + 20)]
		private static void CreateBoxSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "BoxSlider", menuCommand);

		[MenuItem(MenuRoot + "Sliders/MinMaxSlider", false, MenuPriority + 21)]
		private static void CreateMinMaxSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "MinMaxSlider", menuCommand);

		[MenuItem(MenuRoot + "Sliders/RadialSlider", false, MenuPriority + 22)]
		private static void CreateRadialSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "RadialSlider", menuCommand);

		[MenuItem(MenuRoot + "Sliders/RangeSlider", false, MenuPriority + 23)]
		private static void CreateRangeSlider(MenuCommand menuCommand) => CreateVariationHandler("Slider", "RangeSlider", menuCommand);

		[MenuItem(MenuRoot + "Tooltip/Tooltip", false, MenuPriority + 30)]
		private static void CreateTooltip(MenuCommand menuCommand) => CreateWidgetFromAsset("Tooltip", menuCommand);

		[MenuItem(MenuRoot + "Tooltip/TooltipTrigger", false, MenuPriority + 31)]
		private static void CreateTooltipTrigger(MenuCommand menuCommand) => CreateVariationHandler("Tooltip", "TooltipTrigger", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Dialog Screen", false, MenuPriority + 40)]
		private static void CreateDialogScreen(MenuCommand menuCommand) => CreateSingletonVariation("Dialog Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Fader Screen", false, MenuPriority + 41)]
		private static void CreateFaderScreen(MenuCommand menuCommand) => CreateSingletonVariation("Fader Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Input Dialog Screen", false, MenuPriority + 42)]
		private static void CreateInputDialogScreen(MenuCommand menuCommand) => CreateSingletonVariation("Input Dialog Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Loading Screen", false, MenuPriority + 43)]
		private static void CreateLoadingScreen(MenuCommand menuCommand) => CreateSingletonVariation("Loading Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Wait Screen", false, MenuPriority + 44)]
		private static void CreateWaitScreen(MenuCommand menuCommand) => CreateSingletonVariation("Wait Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Line Message Screen", false, MenuPriority + 45)]
		private static void CreateLineMessageScreen(MenuCommand menuCommand) => CreateSingletonVariation("Line Message Screen", menuCommand);

		[MenuItem(MenuRoot + "Singletons/Toast Message Canvas", false, MenuPriority + 46)]
		private static void CreateToastMessageCanvas(MenuCommand menuCommand) => CreateSingletonVariation("Toast Message Canvas", menuCommand);

		[MenuItem(MenuRoot + "Singletons/ModalService", false, MenuPriority + 47)]
		private static void CreateModalService(MenuCommand menuCommand) => CreateSingletonVariation("ModalService", menuCommand);

		[MenuItem(MenuRoot + "Singletons/ContextMenu", false, MenuPriority + 48)]
		private static void CreateContextMenu(MenuCommand menuCommand) => CreateSingletonVariation("ContextMenu", menuCommand);

		[MenuItem(MenuRoot + "Singletons/PopupText", false, MenuPriority + 49)]
		private static void CreatePopupText(MenuCommand menuCommand) => CreateSingletonVariation("PopupText", menuCommand);

		#endregion
	}
}
