using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Tints the Hierarchy row of the GameObject a palette tile would spawn under, so designers see
	/// the destination before they click/drag. Driven by the palette on tile hover
	/// (<see cref="Set"/> / <see cref="Clear"/>).
	/// </summary>
	[InitializeOnLoad]
	internal static class UIWidgetsHierarchyHighlight
	{
		private static int _targetInstanceId;
		private static readonly Color HighlightFill = new Color(0.30f, 0.60f, 1f, 0.22f);
		private static readonly Color HighlightEdge = new Color(0.30f, 0.60f, 1f, 0.85f);

		static UIWidgetsHierarchyHighlight()
		{
			EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItem;
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItem;
		}

		public static void Set(GameObject go)
		{
			int id = go != null ? go.GetInstanceID() : 0;
			if (id == _targetInstanceId) return;
			_targetInstanceId = id;
			EditorApplication.RepaintHierarchyWindow();
		}

		public static void Clear()
		{
			if (_targetInstanceId == 0) return;
			_targetInstanceId = 0;
			EditorApplication.RepaintHierarchyWindow();
		}

		private static void OnHierarchyItem(int instanceId, Rect selectionRect)
		{
			if (_targetInstanceId == 0 || instanceId != _targetInstanceId)
				return;

			// Full-row rect (the passed rect starts after the fold arrow / indent).
			Rect row = new Rect(0, selectionRect.y, selectionRect.xMax + 16f, selectionRect.height);
			EditorGUI.DrawRect(row, HighlightFill);

			// Left accent so it reads even over selected rows.
			EditorGUI.DrawRect(new Rect(row.x, row.y, 2f, row.height), HighlightEdge);
		}
	}
}
