using System.Linq;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.Extensions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// View-agnostic backend for the UI Widgets palette: create/instantiate actions, scene-view
	/// drop handling, canvas resolution and selection helpers. The visual layer (tile grid) lives
	/// in UIWidgetsWindow.View.cs and drives everything here.
	/// </summary>
	public partial class UIWidgets
	{
		public const string DragGenericDataKey = "UIWidgets.Source";
		public const string DragNoCanvasKey = "UIWidgets.NoCanvasRequired";
		private const string UncategorizedLabel = "Uncategorized";

		public UIWidgetsAssetScriptable uiWidgetsAsset;

		private static string GetCategoryDisplayName(string category)
			=> string.IsNullOrEmpty(category) ? UncategorizedLabel : category;

		private bool NameMatches(string key)
		{
			if (string.IsNullOrEmpty(searchQuery)) return true;
			if (string.IsNullOrEmpty(key)) return false;
			return key.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0;
		}

		#region Create actions (used by tiles + context menu)

		/// <summary>Primary action. Creates the widget as a child of the current selection; with no
		/// selection it resolves/creates a Canvas (unless the widget needs no canvas). Mirrors the
		/// native right-click "create" flow designers expect.</summary>
		internal void CreateAsChild(GameObject prefab, bool noCanvasRequired)
		{
			if (prefab == null) return;
			Transform parent = Selection.activeTransform;
			if (parent == null && !noCanvasRequired)
			{
				parent = ResolveCanvasParent();
				if (parent == null) return;
			}
			EnsureEventSystemExists();
			FinalizeCreated(InstantiatePrefabOrClone(prefab, parent), prefab);
		}

		/// <summary>Stock Default UI create (no prefab). Same parenting rules as prefab create.</summary>
		internal void CreateAsChild(System.Func<GameObject> factory, bool noCanvasRequired, string displayName)
		{
			if (factory == null) return;
			Transform parent = Selection.activeTransform;
			if (parent == null && !noCanvasRequired)
			{
				parent = ResolveCanvasParent();
				if (parent == null) return;
			}
			EnsureEventSystemExists();
			GameObject itemObject = factory();
			if (itemObject == null) return;
			Undo.RegisterCreatedObjectUndo(itemObject, "Create " + displayName);
			if (parent != null)
				Undo.SetTransformParent(itemObject.transform, parent, "Parent " + displayName);
			itemObject.name = itemObject.name.RemoveCloneSuffix();
			if (useAutoNaming && uiWidgetsAsset != null)
			{
				itemObject.name = displayName + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);
		}

		/// <summary>Advanced action. Creates the widget beside the selection (same parent).</summary>
		internal void CreateAsSibling(GameObject prefab, bool noCanvasRequired)
		{
			if (prefab == null) return;
			Transform parent = null;
			if (!noCanvasRequired)
			{
				if (Selection.activeTransform == null)
				{
					DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: select a UI object to add a sibling.");
					return;
				}
				parent = Selection.activeTransform.parent;
			}
			EnsureEventSystemExists();
			FinalizeCreated(InstantiatePrefabOrClone(prefab, parent), prefab);
		}

		internal void CreateAsSibling(System.Func<GameObject> factory, bool noCanvasRequired, string displayName)
		{
			if (factory == null) return;
			Transform parent = null;
			if (!noCanvasRequired)
			{
				if (Selection.activeTransform == null)
				{
					DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: select a UI object to add a sibling.");
					return;
				}
				parent = Selection.activeTransform.parent;
			}
			EnsureEventSystemExists();
			GameObject itemObject = factory();
			if (itemObject == null) return;
			Undo.RegisterCreatedObjectUndo(itemObject, "Create " + displayName);
			if (parent != null)
				Undo.SetTransformParent(itemObject.transform, parent, "Parent " + displayName);
			itemObject.name = itemObject.name.RemoveCloneSuffix();
			if (useAutoNaming && uiWidgetsAsset != null)
			{
				itemObject.name = displayName + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);
		}

		/// <summary>Advanced action. Wraps the selection: the new widget takes the selection's slot
		/// and the selection is re-parented under it. Falls back to child when nothing valid is
		/// selected.</summary>
		internal void CreateAsParent(GameObject prefab, bool noCanvasRequired)
		{
			if (prefab == null) return;
			Transform sel = Selection.activeTransform;
			if (sel == null || sel.GetComponent<RectTransform>() == null)
			{
				CreateAsChild(prefab, noCanvasRequired);
				return;
			}

			Transform oldParent = sel.parent;
			int siblingIndex = sel.GetSiblingIndex();

			EnsureEventSystemExists();
			GameObject itemObject = InstantiatePrefabOrClone(prefab, oldParent);
			if (itemObject == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: failed to instantiate '{Name}'.", prefab.name);
				return;
			}

			Undo.RegisterCreatedObjectUndo(itemObject, $"Create {itemObject.name}");
			itemObject.name = itemObject.name.RemoveCloneSuffix();
			itemObject.transform.SetSiblingIndex(siblingIndex);
			Undo.SetTransformParent(sel, itemObject.transform, "Wrap under new parent");

			if (useAutoNaming && uiWidgetsAsset != null)
			{
				itemObject.name = prefab.name + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);
		}

		internal void CreateAsParent(System.Func<GameObject> factory, bool noCanvasRequired, string displayName)
		{
			if (factory == null) return;
			Transform sel = Selection.activeTransform;
			if (sel == null || sel.GetComponent<RectTransform>() == null)
			{
				CreateAsChild(factory, noCanvasRequired, displayName);
				return;
			}

			Transform oldParent = sel.parent;
			int siblingIndex = sel.GetSiblingIndex();

			EnsureEventSystemExists();
			GameObject itemObject = factory();
			if (itemObject == null) return;

			Undo.RegisterCreatedObjectUndo(itemObject, "Create " + displayName);
			if (oldParent != null)
				Undo.SetTransformParent(itemObject.transform, oldParent, "Parent " + displayName);
			itemObject.transform.SetSiblingIndex(siblingIndex);
			Undo.SetTransformParent(sel, itemObject.transform, "Wrap under new parent");

			if (useAutoNaming && uiWidgetsAsset != null)
			{
				itemObject.name = displayName + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);
		}

		private GameObject InstantiatePrefabOrClone(GameObject prefab, Transform parent)
		{
			if (prefab == null) return null;
			if (isInstantiatingPrefab)
				return (parent != null
					? PrefabUtility.InstantiatePrefab(prefab, parent)
					: PrefabUtility.InstantiatePrefab(prefab)) as GameObject;
			return parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
		}

		private void FinalizeCreated(GameObject itemObject, GameObject prefab)
		{
			if (itemObject == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: failed to instantiate '{Name}'.", prefab != null ? prefab.name : "null");
				return;
			}

			Undo.RegisterCreatedObjectUndo(itemObject, $"Create {itemObject.name}");
			itemObject.name = itemObject.name.RemoveCloneSuffix();

			if (useAutoNaming && uiWidgetsAsset != null)
			{
				itemObject.name = (prefab != null ? prefab.name : itemObject.name) + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);
		}

		private Transform ResolveCanvasParent()
		{
			Transform parent = null;
			if (preferExistingCanvas)
				parent = FindFirstObjectByType<Canvas>()?.transform;
			if (parent == null)
				parent = CreateCanvasExplicit()?.transform;
			if (parent == null)
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: no Canvas available.");
			return parent;
		}

		#endregion

		#region Attach-component actions (Utility / Effects / Primitives sections)

		private GameObject GetTargetOrCreateCanvasChild(string objectName)
		{
			if (Selection.activeGameObject != null)
				return Selection.activeGameObject;

			Transform canvas = ResolveCanvasParent();
			if (canvas == null)
				return null;

			var child = new GameObject(objectName);
			child.transform.SetParent(canvas);
			Undo.RegisterCreatedObjectUndo(child, "Create " + objectName);
			return child;
		}

		internal void AttachOrAddToCanvas(System.Type componentType, string displayName)
		{
			var target = GetTargetOrCreateCanvasChild(displayName);
			if (target == null)
				return;
			if (target.GetComponent(componentType) != null)
				return;
			Undo.AddComponent(target, componentType);
			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(target, target);
		}

		#endregion

		#region Selection helpers (Selection Tools section)

		internal void SelectButtonsWith<TText>(bool inChildren) where TText : Component
		{
			Transform root = inChildren ? Selection.activeTransform : null;
			Selection.objects = GetAllT2FromT1<Button, TText>(true, root).Select(x => x.gameObject).ToArray();
		}

		#endregion

		#region Scene view

		/// <summary>Enables Scene View 2D mode (if needed) and frames the selected UI object.</summary>
		internal static void FocusSelectedUIIn2D()
		{
			var go = Selection.activeGameObject;
			if (go == null || go.GetComponent<RectTransform>() == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: select a UI object to focus in 2D.");
				return;
			}

			var sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null && SceneView.sceneViews.Count > 0)
				sceneView = SceneView.sceneViews[0] as SceneView;
			if (sceneView == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: no Scene View available.");
				return;
			}

			if (!sceneView.in2DMode)
				sceneView.in2DMode = true;

			sceneView.FrameSelected();
			sceneView.Focus();
		}

		#endregion

		#region Scene-view drop

		internal void HandleSceneViewDragDrop(SceneView sceneView)
		{
			Event e = Event.current;
			if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
				return;

			if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length != 1)
				return;
			if (!(DragAndDrop.objectReferences[0] is GameObject prefab))
				return;
			if (DragAndDrop.GetGenericData(DragGenericDataKey) == null)
				return;

			bool noCanvasRequired = DragAndDrop.GetGenericData(DragNoCanvasKey) is bool b && b;

			if (e.type == EventType.DragUpdated)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Link;
				e.Use();
				return;
			}

			if (e.type != EventType.DragPerform)
				return;

			GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
			Transform parent = null;
			RectTransform rectParent = null;
			Vector2 localPoint = Vector2.zero;
			Camera cam = sceneView != null ? sceneView.camera : null;

			if (picked != null)
			{
				RectTransform rt = picked.GetComponent<RectTransform>();
				if (rt != null)
				{
					rectParent = rt;
					parent = rt;
				}
				else
				{
					Canvas canvas = picked.GetComponentInParent<Canvas>();
					if (canvas != null)
					{
						parent = canvas.transform;
						rectParent = canvas.GetComponent<RectTransform>();
					}
				}
			}

			if (parent == null && !noCanvasRequired)
			{
				if (preferExistingCanvas)
					parent = FindFirstObjectByType<Canvas>()?.transform;
				if (parent == null)
				{
					GameObject newCanvas = CreateCanvasExplicit();
					if (newCanvas == null)
					{
						DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: failed to create Canvas for drop.");
						e.Use();
						return;
					}
					parent = newCanvas.transform;
					rectParent = newCanvas.GetComponent<RectTransform>();
				}
			}

			if (parent == null && !noCanvasRequired)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: no Canvas for drop.");
				e.Use();
				return;
			}

			if (parent != null && rectParent != null)
			{
				Vector2 screenPos = HandleUtility.GUIPointToScreenPixelCoordinate(e.mousePosition);
				RectTransformUtility.ScreenPointToLocalPointInRectangle(rectParent, screenPos, cam, out localPoint);
			}

			GameObject itemObject;
			if (parent == null)
			{
				itemObject = isInstantiatingPrefab
					? PrefabUtility.InstantiatePrefab(prefab) as GameObject
					: Instantiate(prefab);
			}
			else
			{
				itemObject = isInstantiatingPrefab
					? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject
					: Instantiate(prefab, parent);
			}

			if (itemObject == null)
			{
				DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UIWidgets: failed to instantiate prefab on drop.");
				e.Use();
				return;
			}

			Undo.RegisterCreatedObjectUndo(itemObject, "Create " + prefab.name);
			itemObject.name = itemObject.name.RemoveCloneSuffix();

			if (rectParent != null)
			{
				RectTransform itemRect = itemObject.GetComponent<RectTransform>();
				if (itemRect != null)
					itemRect.anchoredPosition = localPoint;
			}

			if (useAutoNaming)
			{
				itemObject.name = prefab.name + uiWidgetsAsset.nameDelimiter;
				RenameGameObjectModal.Open(itemObject, itemObject.name);
			}

			if (autoSelectNewItems)
				Selection.SetActiveObjectWithContext(itemObject, itemObject);

			DragAndDrop.AcceptDrag();
			e.Use();
		}

		#endregion

		#region Canvas + selection utilities

		private GameObject CreateCanvasExplicit(Transform parent = null)
		{
			if (parent == null)
				parent = Selection.activeTransform;

			GameObject canvasGO = new GameObject("Canvas");
			canvasGO.layer = LayerMask.NameToLayer("UI");
			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();
			if (parent != null)
				canvasGO.transform.SetParent(parent, false);
			Undo.RegisterCreatedObjectUndo(canvasGO, "Create new Canvas");
			return canvasGO;
		}

		private static void EnsureEventSystemExists()
		{
			if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
				return;

			var es = new GameObject("EventSystem");
			es.AddComponent<UnityEngine.EventSystems.EventSystem>();
			es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
			Undo.RegisterCreatedObjectUndo(es, "Create new EventSystem");
		}

		private static T[] FindObjectsOfTypeInChildrenRecursive<T>(Transform root, bool includeInactive = true)
			where T : Component
		{
			System.Collections.Generic.List<T> results = new System.Collections.Generic.List<T>();

			foreach (Transform child in root)
			{
				var comp = child.GetComponent(typeof(T));
				if (comp != null && (includeInactive || child.gameObject.activeInHierarchy))
					results.Add((T)comp);

				results.AddRange(FindObjectsOfTypeInChildrenRecursive<T>(child, includeInactive));
			}

			return results.ToArray();
		}

		private static System.Collections.Generic.List<T2> GetAllT2FromT1<T1, T2>(bool includeInactive = true, Transform root = null)
			where T1 : Component
			where T2 : Component
		{
			System.Collections.Generic.List<T2> list = new();

			if (root != null)
			{
				list = FindObjectsOfTypeInChildrenRecursive<T2>(root, includeInactive).ToList();
			}
			else
			{
				T1[] t1Arr = FindObjectsByType<T1>(
					includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
					FindObjectsSortMode.None);

				foreach (T1 t1 in t1Arr)
				{
					if (t1.transform.childCount > 0)
					{
						Transform firstChild = t1.transform.GetChild(0);
						if (firstChild.GetComponent<T2>() == null)
							continue;
						list.Add(firstChild.GetComponent<T2>());
					}
				}
			}

			return list;
		}

		#endregion
	}
}
