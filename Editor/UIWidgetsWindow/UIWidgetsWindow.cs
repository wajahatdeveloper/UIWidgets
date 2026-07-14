using AetherNexus.FoundationPlatform.Editor.Utilities;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	public partial class UIWidgets : EditorWindow
	{
		private static Vector2 _WindowMinSize = new Vector2(250f, 200f);

		private const string PrefKey_IsInstantiatingPrefab = "UIWidgets.IsInstantiatingPrefab";
		private const string PrefKey_AutoSelectNewItems = "UIWidgets.AutoSelectNewItems";
		private const string PrefKey_PreferExistingCanvas = "UIWidgets.PreferExistingCanvas";
		private const string PrefKey_UseAutoNaming = "UIWidgets.UseAutoNaming";
		private const string PrefKey_SearchQuery = "UIWidgets.SearchQuery";

		private bool isInstantiatingPrefab = true;
		private bool autoSelectNewItems = true;
		private bool preferExistingCanvas = false;
		private bool useAutoNaming = false;
		private string searchQuery = string.Empty;

		[MenuItem(MenuPaths.UIWidgets.WidgetsWindow, false, 1101)]
		[MenuItem(MenuPaths.UIWidgets.GameObjectOpen, false, 0)]
		public static void Init()
		{
			var window = GetWindow<UIWidgets>();
			window.titleContent = new GUIContent("UI Widgets");
			window.minSize = _WindowMinSize;
			window.Show();
		}

		private void OnEnable()
		{
			isInstantiatingPrefab = EditorPrefs.GetBool(PrefKey_IsInstantiatingPrefab, true);
			autoSelectNewItems = EditorPrefs.GetBool(PrefKey_AutoSelectNewItems, true);
			preferExistingCanvas = EditorPrefs.GetBool(PrefKey_PreferExistingCanvas, false);
			useAutoNaming = EditorPrefs.GetBool(PrefKey_UseAutoNaming, false);
			searchQuery = EditorPrefs.GetString(PrefKey_SearchQuery, string.Empty);
			// Idempotent: the palette can be instanced both as the floating window and as the
			// hidden instance backing the scene overlay. Only one scene-drag handler must exist.
			SceneView.duringSceneGui -= OnSceneViewGUI;
			SceneView.duringSceneGui += OnSceneViewGUI;
		}

		private void OnDisable()
		{
			SceneView.duringSceneGui -= OnSceneViewGUI;
			EditorPrefs.SetBool(PrefKey_IsInstantiatingPrefab, isInstantiatingPrefab);
			EditorPrefs.SetBool(PrefKey_AutoSelectNewItems, autoSelectNewItems);
			EditorPrefs.SetBool(PrefKey_PreferExistingCanvas, preferExistingCanvas);
			EditorPrefs.SetBool(PrefKey_UseAutoNaming, useAutoNaming);
			EditorPrefs.SetString(PrefKey_SearchQuery, searchQuery ?? string.Empty);
		}

		private static void OnSceneViewGUI(SceneView sceneView)
		{
			// Resolve an existing instance (floating window or overlay-hidden) WITHOUT creating
			// or showing the floating window. GetWindow<> would pop the window on the first drag.
			var instances = Resources.FindObjectsOfTypeAll<UIWidgets>();
			if (instances == null || instances.Length == 0)
				return;
			UIWidgets window = instances[0];
			if (window == null)
				return;
			window.EnsureAssetLoaded();
			if (window.uiWidgetsAsset == null)
				return;
			window.HandleSceneViewDragDrop(sceneView);
		}

		internal static bool GetIsInstantiatingPrefab()
		{
			return EditorPrefs.GetBool(PrefKey_IsInstantiatingPrefab, true);
		}

		/// <summary>
		/// Builds the UIElements palette into the window root. The identical tree is also hosted by
		/// the Scene View overlay via <see cref="BuildPaletteRoot"/>.
		/// </summary>
		private void CreateGUI()
		{
			EnsureAssetLoaded();
			rootVisualElement.Add(BuildPaletteRoot());
		}

		private void OnFocus()
		{
			if (uiWidgetsAsset == null)
			{
				TryLoadAsset();
				if (uiWidgetsAsset != null)
					Resources.UnloadUnusedAssets();
			}
		}

		/// <summary>Loads the widget asset if not already resolved. Safe to call every frame.</summary>
		internal void EnsureAssetLoaded()
		{
			if (uiWidgetsAsset == null)
				TryLoadAsset();
		}

		private void TryLoadAsset()
		{
			var assets = EditorAssetScanCache.GetAssets<UIWidgetsAssetScriptable>();
			if (assets.Count > 0)
			{
				uiWidgetsAsset = assets[0];
			}
		}
	}
}