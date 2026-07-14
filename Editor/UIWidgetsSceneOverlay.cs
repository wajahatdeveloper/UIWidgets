using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Docks the full UI Widgets palette into the Scene View as a Visual-Studio-style toolbox.
	/// Auto-shows (ITransientOverlay) whenever a UI object / UI prefab stage is in context, so
	/// designers get the widget list beside the scene without hunting for a floating window.
	/// Hosts a hidden UIWidgets instance and adds its UIElements palette tree to the overlay.
	/// </summary>
	[Overlay(typeof(SceneView), "UI Widgets", true)]
	public class UIWidgetsSceneOverlay : Overlay, ITransientOverlay
	{
		private UIWidgets _palette;
		private UnityEngine.UIElements.Button _editPrefabButton;

		public bool visible => IsUIEditingContext();

		public override VisualElement CreatePanelContent()
		{
			var root = new VisualElement { name = "UIWidgetsOverlayRoot" };
			root.style.minWidth = 270;

			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;

			_editPrefabButton = new UnityEngine.UIElements.Button(OpenPrefabForEditing)
			{
				text = "Edit Prefab",
				tooltip = "Open the selected prefab instance for editing"
			};
			_editPrefabButton.style.flexGrow = 1;
			_editPrefabButton.style.display = ShouldShowOpenPrefabButton() ? DisplayStyle.Flex : DisplayStyle.None;
			header.Add(_editPrefabButton);

			var fullWindow = new UnityEngine.UIElements.Button(OpenFullWindow)
			{
				text = "Full Window",
				tooltip = "Open the full UI Widgets window as a floating panel"
			};
			fullWindow.style.flexGrow = 1;
			header.Add(fullWindow);
			root.Add(header);

			var palette = GetPalette().BuildPaletteRoot();
			palette.style.minHeight = 320;
			// Cap height so the palette's ScrollView bounds and scrolls instead of growing the
			// overlay unboundedly when categories expand.
			palette.style.maxHeight = 620;
			palette.style.flexGrow = 1;
			root.Add(palette);

			Selection.selectionChanged += OnSelectionChanged;
			return root;
		}

		public override void OnWillBeDestroyed()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			if (_palette != null)
			{
				Object.DestroyImmediate(_palette);
				_palette = null;
			}
			base.OnWillBeDestroyed();
		}

		private void OnSelectionChanged()
		{
			var sv = SceneView.lastActiveSceneView;
			if (sv != null) sv.Repaint();
			if (_editPrefabButton != null)
				_editPrefabButton.style.display = ShouldShowOpenPrefabButton() ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Lazily creates a hidden UIWidgets instance to back the overlay. Kept separate from the
		/// floating window so closing one never dangles the other; both share widget-asset state
		/// and EditorPrefs-persisted toggles.
		/// </summary>
		private UIWidgets GetPalette()
		{
			if (_palette == null)
			{
				_palette = ScriptableObject.CreateInstance<UIWidgets>();
				_palette.hideFlags = HideFlags.HideAndDontSave;
			}
			_palette.EnsureAssetLoaded();
			return _palette;
		}

		private static bool ShouldShowOpenPrefabButton()
		{
			if (PrefabStageUtility.GetCurrentPrefabStage() != null)
				return false;
			var go = Selection.activeGameObject;
			if (go == null || go.GetComponent<RectTransform>() == null)
				return false;
			return PrefabUtility.IsPartOfPrefabInstance(go);
		}

		private static void OpenPrefabForEditing()
		{
			var go = Selection.activeGameObject;
			if (go == null || !PrefabUtility.IsPartOfPrefabInstance(go))
				return;
			var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
			if (instanceRoot == null)
				return;
			var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot) as GameObject;
			if (prefabAsset == null)
				return;
			var path = AssetDatabase.GetAssetPath(prefabAsset);
			if (string.IsNullOrEmpty(path))
				return;
			PrefabStageUtility.OpenPrefab(path, instanceRoot);
		}

		private static bool IsUIEditingContext()
		{
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.prefabContentsRoot != null &&
			    stage.prefabContentsRoot.GetComponent<RectTransform>() != null)
				return true;
			var go = Selection.activeGameObject;
			return go != null && go.GetComponent<RectTransform>() != null;
		}

		private static void OpenFullWindow()
		{
			UIWidgets.Init();
			EditorWindow.FocusWindowIfItsOpen<UIWidgets>();
		}
	}
}
