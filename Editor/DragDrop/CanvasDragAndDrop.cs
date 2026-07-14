using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Drag a Sprite, Texture, or UI prefab from the Project window into the Scene View
	/// over a Canvas: Sprite → Image, Texture → RawImage, RectTransform-prefab → instance,
	/// created under the hovered RectTransform at the drop point. Non-UI payloads are
	/// ignored so Unity's default scene drag keeps working.
	/// </summary>
	[InitializeOnLoad]
	public static class CanvasDragAndDrop
	{
		private enum PayloadKind { None, Sprite, Texture, UiPrefab }

		static CanvasDragAndDrop()
		{
			SceneView.duringSceneGui += OnSceneGui;
		}

		private static void OnSceneGui(SceneView view)
		{
			var settings = UIWidgetsSettings.instance;
			if (!settings.CanvasDragDropEnabled)
				return;

			var e = Event.current;
			if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform)
				return;

			var kind = Classify(out var payload);
			if (kind == PayloadKind.None)
				return;

			var target = FindTargetRect(e.mousePosition, out var camera);
			if (target == null)
				return;

			if (e.type == EventType.DragUpdated)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				DrawTargetHighlight(target);
				e.Use();
				return;
			}

			DragAndDrop.AcceptDrag();
			var created = Create(kind, payload, target, camera, e.mousePosition, settings);
			if (created != null)
			{
				Undo.RegisterCreatedObjectUndo(created, "Drop UI Element");
				if (settings.DragDropSelectsCreated)
					Selection.activeGameObject = created;
			}
			e.Use();
		}

		private static PayloadKind Classify(out Object payload)
		{
			payload = null;
			var refs = DragAndDrop.objectReferences;
			if (refs == null || refs.Length != 1)
				return PayloadKind.None;

			payload = refs[0];
			switch (payload)
			{
				case Sprite _:
					return PayloadKind.Sprite;
				case Texture2D texture:
					// A sprite-typed texture drops as an Image using its primary sprite.
					var path = AssetDatabase.GetAssetPath(texture);
					if (!string.IsNullOrEmpty(path))
					{
						var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
						if (sprite != null)
						{
							payload = sprite;
							return PayloadKind.Sprite;
						}
					}
					return PayloadKind.Texture;
				case RenderTexture _:
					return PayloadKind.Texture;
				case GameObject go when PrefabUtility.IsPartOfPrefabAsset(go) && go.transform is RectTransform:
					return PayloadKind.UiPrefab;
				default:
					return PayloadKind.None;
			}
		}

		/// <summary>Deepest RectTransform under the Scene View mouse position, canvas roots as fallback.</summary>
		private static RectTransform FindTargetRect(Vector2 guiPosition, out Camera eventCamera)
		{
			eventCamera = null;
			var screenPoint = HandleUtility.GUIPointToScreenPixelCoordinate(guiPosition);

			RectTransform best = null;
			var bestDepth = -1;

			foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (!canvas.isRootCanvas)
					continue;
				var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

				foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(false))
				{
					if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, camera))
						continue;
					var depth = Depth(rt);
					if (depth > bestDepth)
					{
						bestDepth = depth;
						best = rt;
						eventCamera = camera;
					}
				}
			}
			return best;
		}

		private static int Depth(Transform t)
		{
			var depth = 0;
			while (t.parent != null) { depth++; t = t.parent; }
			return depth;
		}

		private static void DrawTargetHighlight(RectTransform target)
		{
			var corners = new Vector3[4];
			target.GetWorldCorners(corners);
			Handles.DrawSolidRectangleWithOutline(corners,
				new Color(0.3f, 0.6f, 1f, 0.08f), new Color(0.3f, 0.6f, 1f, 0.9f));
		}

		private static GameObject Create(PayloadKind kind, Object payload, RectTransform parent,
			Camera camera, Vector2 guiPosition, UIWidgetsSettings settings)
		{
			GameObject created;
			switch (kind)
			{
				case PayloadKind.Sprite: {
					created = new GameObject(payload.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
					var image = created.GetComponent<Image>();
					image.sprite = (Sprite)payload;
					break;
				}
				case PayloadKind.Texture: {
					created = new GameObject(payload.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
					created.GetComponent<RawImage>().texture = (Texture)payload;
					break;
				}
				case PayloadKind.UiPrefab:
					created = (GameObject)PrefabUtility.InstantiatePrefab(payload);
					break;
				default:
					return null;
			}

			var rt = (RectTransform)created.transform;
			rt.SetParent(parent, false);

			var screenPoint = HandleUtility.GUIPointToScreenPixelCoordinate(guiPosition);
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out var local))
				rt.anchoredPosition = local;

			if (settings.DragDropSetNativeSize)
			{
				var image = created.GetComponent<Image>();
				if (image != null && image.sprite != null)
					image.SetNativeSize();
				var raw = created.GetComponent<RawImage>();
				if (raw != null && raw.texture != null)
					raw.SetNativeSize();
			}
			return created;
		}
	}
}
