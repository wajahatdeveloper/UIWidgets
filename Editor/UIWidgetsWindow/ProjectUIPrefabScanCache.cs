using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Cached scan of prefab assets whose root GameObject is on Unity's UI layer.
	/// Curated UIWidgetsAsset entries are filtered out by the palette when building tiles.
	/// </summary>
	internal static class ProjectUIPrefabScanCache
	{
		internal readonly struct Entry
		{
			public readonly string guid;
			public readonly GameObject prefab;
			public readonly bool noCanvasRequired;

			public Entry(string guid, GameObject prefab, bool noCanvasRequired)
			{
				this.guid = guid;
				this.prefab = prefab;
				this.noCanvasRequired = noCanvasRequired;
			}

			public string Name => prefab != null ? prefab.name : string.Empty;
		}

		private static readonly List<Entry> _entries = new List<Entry>();
		private static bool _dirty = true;
		private static int _uiLayer = -1;

		internal static event Action Invalidated;

		internal static IReadOnlyList<Entry> GetEntries()
		{
			if (_dirty)
				Rebuild();
			return _entries;
		}

		internal static void Invalidate()
		{
			_dirty = true;
			Invalidated?.Invoke();
		}

		private static void Rebuild()
		{
			_entries.Clear();
			_dirty = false;

			if (_uiLayer < 0)
				_uiLayer = LayerMask.NameToLayer("UI");
			if (_uiLayer < 0)
				return;

			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
			{
				if (!seen.Add(guid))
					continue;

				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
					continue;

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null || prefab.layer != _uiLayer)
					continue;

				_entries.Add(new Entry(guid, prefab, ResolveNoCanvasRequired(prefab)));
			}

			_entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
		}

		private static bool ResolveNoCanvasRequired(GameObject prefab)
		{
			if (prefab.GetComponent<Canvas>() != null)
				return true;
			return prefab.GetComponent<RectTransform>() == null;
		}

		private class ProjectUIPrefabAssetWatcher : AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(
				string[] importedAssets,
				string[] deletedAssets,
				string[] movedAssets,
				string[] movedFromAssetPaths)
			{
				Invalidate();
			}
		}
	}

}
