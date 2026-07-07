using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
	/// <summary>
	/// RectTransform anchor helpers. "Fit" moves the anchors onto the rect's current corners
	/// so the element scales with its parent, leaving offsets at zero.
	/// </summary>
	public static class AnchorTools
	{
		[MenuItem("Tools/UIWidgets/Fit Anchors &o", false, 901)]
		public static void FitAnchorsToCorners()
		{
			foreach (var go in Selection.gameObjects)
			{
				if (go.transform is not RectTransform rect || rect.parent is not RectTransform parent)
				{
					continue;
				}

				var parentSize = parent.rect.size;
				if (parentSize.x <= 0f || parentSize.y <= 0f)
				{
					continue;
				}

				Undo.RecordObject(rect, "Fit Anchors");

				rect.anchorMin = new Vector2(
					rect.anchorMin.x + (rect.offsetMin.x / parentSize.x),
					rect.anchorMin.y + (rect.offsetMin.y / parentSize.y));
				rect.anchorMax = new Vector2(
					rect.anchorMax.x + (rect.offsetMax.x / parentSize.x),
					rect.anchorMax.y + (rect.offsetMax.y / parentSize.y));
				rect.offsetMin = Vector2.zero;
				rect.offsetMax = Vector2.zero;

				EditorUtility.SetDirty(rect);
			}
		}
	}
}
