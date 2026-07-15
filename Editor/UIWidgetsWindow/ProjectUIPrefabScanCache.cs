using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Cached scan of prefab assets under Assets/ whose root GameObject is on Unity's UI layer.
	/// Curated UIWidgetsAsset entries are filtered out by the palette when building tiles.
	/// Rebuild is incremental across editor update ticks so overlay / window open stays responsive.
	/// </summary>
	internal static class ProjectUIPrefabScanCache
	{
		private const int WarmBudgetMs = 4;

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
		private static List<Entry> _staging;
		private static HashSet<string> _stagingSeen;
		private static string[] _pendingGuids;
		private static int _pendingIndex;
		private static bool _dirty = true;
		private static bool _warming;
		private static int _uiLayer = -1;

		internal static event Action Invalidated;
		internal static event Action Warmed;

		/// <summary>True when the cache has a completed scan and none is in progress.</summary>
		internal static bool IsReady => !_dirty && !_warming;

		[InitializeOnLoadMethod]
		private static void WarmOnEditorLoad()
		{
			EditorApplication.delayCall += RequestWarm;
		}

		/// <summary>
		/// Returns the last completed scan. If dirty, schedules a background warm and returns
		/// the current list (empty after invalidate until warm finishes).
		/// </summary>
		internal static IReadOnlyList<Entry> GetEntries()
		{
			if (_dirty)
				RequestWarm();
			return _entries;
		}

		internal static void RequestWarm()
		{
			if (!_dirty || _warming)
				return;
			StartWarm();
		}

		internal static void Invalidate()
		{
			CancelWarm();
			_dirty = true;
			_entries.Clear();
			Invalidated?.Invoke();
		}

		private static void StartWarm()
		{
			_warming = true;
			_staging = new List<Entry>();
			_stagingSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			_pendingGuids = null;
			_pendingIndex = 0;

			if (_uiLayer < 0)
				_uiLayer = LayerMask.NameToLayer("UI");

			EditorApplication.update -= WarmTick;
			EditorApplication.update += WarmTick;
		}

		private static void CancelWarm()
		{
			if (!_warming)
				return;
			EditorApplication.update -= WarmTick;
			_warming = false;
			_staging = null;
			_stagingSeen = null;
			_pendingGuids = null;
			_pendingIndex = 0;
		}

		private static void WarmTick()
		{
			if (_uiLayer < 0)
			{
				FinishWarm(Array.Empty<Entry>());
				return;
			}

			var sw = Stopwatch.StartNew();

			if (_pendingGuids == null)
			{
				_pendingGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
				_pendingIndex = 0;
				if (sw.ElapsedMilliseconds >= WarmBudgetMs)
					return;
			}

			while (_pendingIndex < _pendingGuids.Length)
			{
				if (sw.ElapsedMilliseconds >= WarmBudgetMs)
					return;

				var guid = _pendingGuids[_pendingIndex++];
				if (!_stagingSeen.Add(guid))
					continue;

				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path) ||
				    !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
					continue;

				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null || prefab.layer != _uiLayer)
					continue;

				_staging.Add(new Entry(guid, prefab, ResolveNoCanvasRequired(prefab)));
			}

			_staging.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			FinishWarm(_staging.ToArray());
		}

		private static void FinishWarm(Entry[] completed)
		{
			EditorApplication.update -= WarmTick;
			_entries.Clear();
			_entries.AddRange(completed);
			_staging = null;
			_stagingSeen = null;
			_pendingGuids = null;
			_pendingIndex = 0;
			_warming = false;
			_dirty = false;
			Warmed?.Invoke();
		}

		private static bool ResolveNoCanvasRequired(GameObject prefab)
		{
			if (prefab.GetComponent<Canvas>() != null)
				return true;
			return prefab.GetComponent<RectTransform>() == null;
		}

		private static bool TouchesPrefab(string path) =>
			!string.IsNullOrEmpty(path) &&
			path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
			path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);

		private class ProjectUIPrefabAssetWatcher : AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(
				string[] importedAssets,
				string[] deletedAssets,
				string[] movedAssets,
				string[] movedFromAssetPaths)
			{
				if (TouchesAnyPrefab(importedAssets) ||
				    TouchesAnyPrefab(deletedAssets) ||
				    TouchesAnyPrefab(movedAssets) ||
				    TouchesAnyPrefab(movedFromAssetPaths))
				{
					Invalidate();
				}
			}

			private static bool TouchesAnyPrefab(string[] paths)
			{
				if (paths == null) return false;
				for (int i = 0; i < paths.Length; i++)
				{
					if (TouchesPrefab(paths[i]))
						return true;
				}
				return false;
			}
		}
	}
}
