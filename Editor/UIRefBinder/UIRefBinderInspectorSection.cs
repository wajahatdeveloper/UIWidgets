#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	/// <summary>
	/// Draws a "Bind UI From Selection" foldout for a given UIRefBinder field. Invoked exclusively by
	/// UIRefBinderDrawer, so this only ever appears in the Inspector row of whatever
	/// `[SerializeField] private UIRefBinder ...;` field a script declares — never injected elsewhere.
	/// </summary>
	internal static class UIRefBinderInspectorSection
	{
		private sealed class RowState
		{
			public GameObject gameObject;
			public List<Type> candidateTypes;
			public int selectedTypeIndex;
			public bool assignExisting;
			public string existingFieldName = string.Empty;
			public string newFieldName = string.Empty;
		}

		private static readonly Dictionary<(MonoBehaviour view, string path), bool> s_expandedByView =
			new Dictionary<(MonoBehaviour view, string path), bool>();
		private static readonly Dictionary<(MonoBehaviour view, string path, GameObject go), RowState> s_rows =
			new Dictionary<(MonoBehaviour view, string path, GameObject go), RowState>();

		// Parsing a script's [SerializeField] declarations means reading the whole file from disk and
		// running it through regex — too slow to redo on every OnGUI repaint (this foldout redraws
		// constantly while dragging a Hierarchy multi-select). Cached per script path and only
		// re-parsed when the file's last-write time actually changes.
		private static readonly Dictionary<string, (long writeTimeTicks, List<DeclaredFieldInfo> declared)> s_declaredFieldsCache =
			new Dictionary<string, (long, List<DeclaredFieldInfo>)>();

		/// <summary>
		/// Draws the "Bind UI From Selection" foldout for the owning MonoBehaviour of
		/// <paramref name="property"/>. Safe to call from multiple simultaneously-visible drawers
		/// targeting different fields/owners — all cached state below is keyed per (owner, field path).
		/// </summary>
		internal static void Draw(SerializedProperty property)
		{
			if (property == null)
				return;
			var view = property.serializedObject.targetObject as MonoBehaviour;
			if (view == null)
				return;
			string path = property.propertyPath;

			MonoScript script = MonoScript.FromMonoBehaviour(view);
			if (script == null)
				return;
			string scriptPath = AssetDatabase.GetAssetPath(script);
			if (string.IsNullOrEmpty(scriptPath))
				return;

			EditorGUILayout.Space();
			var expandedKey = (view, path);
			bool expanded = s_expandedByView.TryGetValue(expandedKey, out var isExpanded) && isExpanded;
			expanded = EditorGUILayout.Foldout(expanded, "Bind UI From Selection", true);
			s_expandedByView[expandedKey] = expanded;
			if (!expanded)
				return;

			var selected = new List<GameObject>();
			foreach (var go in Selection.gameObjects)
			{
				if (go != null && go != view.gameObject)
					selected.Add(go);
			}

			EditorGUI.indentLevel++;
			try
			{
				if (selected.Count == 0)
				{
					EditorGUILayout.HelpBox(
						"Lock this Inspector (padlock, top right) and multi-select the Button/Text/Image GameObjects " +
						"in the Hierarchy to bind them to this script.", MessageType.Info);
					return;
				}

				if (!TryGetDeclaredFieldsCached(scriptPath, out List<DeclaredFieldInfo> declaredFields, out string readError))
				{
					EditorGUILayout.HelpBox($"Could not read script source: {readError}", MessageType.Error);
					return;
				}

				List<ExistingFieldInfo> existingFields = ScriptFieldScanner.ToExistingFields(property.serializedObject, declaredFields);
				var unassignedFields = existingFields.Where(f => !f.isAssigned).ToList();

				PruneStaleRows(view, path, selected);

				var claimedExistingFields = new HashSet<string>();
				var claimedNewNames = new HashSet<string>(existingFields.Select(f => f.fieldName));
				var rowsInOrder = new List<RowState>();

				foreach (var go in selected)
				{
					RowState row = GetOrCreateRow(view, path, go, unassignedFields, claimedExistingFields, claimedNewNames);
					rowsInOrder.Add(row);
					DrawRow(row, go, unassignedFields, claimedExistingFields);

					if (row.assignExisting && !string.IsNullOrEmpty(row.existingFieldName))
						claimedExistingFields.Add(row.existingFieldName);
					else if (!string.IsNullOrEmpty(row.newFieldName))
						claimedNewNames.Add(row.newFieldName);
				}

				EditorGUILayout.Space();
				bool canBind = rowsInOrder.Any(r => r.assignExisting
					? !string.IsNullOrEmpty(r.existingFieldName)
					: !string.IsNullOrEmpty(r.newFieldName));

				using (new EditorGUI.DisabledScope(!canBind))
				{
					if (GUILayout.Button("Bind", GUILayout.Height(24)))
					{
						Commit(property, view, path, scriptPath, rowsInOrder);
					}
				}
			}
			finally
			{
				EditorGUI.indentLevel--;
			}
		}

		private static bool TryGetDeclaredFieldsCached(string scriptPath, out List<DeclaredFieldInfo> declaredFields, out string error)
		{
			error = null;
			string fullPath;
			long writeTicks;
			try
			{
				fullPath = Path.GetFullPath(scriptPath);
				writeTicks = File.GetLastWriteTimeUtc(fullPath).Ticks;
			}
			catch (Exception ex)
			{
				declaredFields = null;
				error = ex.Message;
				return false;
			}

			if (s_declaredFieldsCache.TryGetValue(scriptPath, out var cached) && cached.writeTimeTicks == writeTicks)
			{
				declaredFields = cached.declared;
				return true;
			}

			string scriptSource;
			try { scriptSource = File.ReadAllText(fullPath); }
			catch (Exception ex)
			{
				declaredFields = null;
				error = ex.Message;
				return false;
			}

			declaredFields = ScriptFieldScanner.ParseDeclaredFields(scriptSource);
			s_declaredFieldsCache[scriptPath] = (writeTicks, declaredFields);
			return true;
		}

		private static void PruneStaleRows(MonoBehaviour view, string path, List<GameObject> selected)
		{
			if (s_rows.Count == 0)
				return;

			var selectedSet = new HashSet<GameObject>(selected);
			var stale = s_rows.Keys.Where(k => k.view == view && k.path == path && (k.go == null || !selectedSet.Contains(k.go))).ToList();
			foreach (var k in stale)
				s_rows.Remove(k);
		}

		private static RowState GetOrCreateRow(MonoBehaviour view, string path, GameObject go, List<ExistingFieldInfo> unassignedFields,
			HashSet<string> claimedExistingFields, HashSet<string> claimedNewNames)
		{
			var key = (view, path, go);
			if (s_rows.TryGetValue(key, out var existing))
				return existing;

			var row = new RowState { gameObject = go, candidateTypes = UIComponentTypeCatalog.GetCandidateTypes(go) };

			ExistingFieldInfo? bestMatch = null;
			foreach (var f in unassignedFields)
			{
				if (claimedExistingFields.Contains(f.fieldName))
					continue;
				if (!UIComponentTypeCatalog.HasReference(go, f.fieldType))
					continue;
				bestMatch = f;
				break;
			}

			if (bestMatch.HasValue)
			{
				row.assignExisting = true;
				row.existingFieldName = bestMatch.Value.fieldName;
			}
			else
			{
				row.assignExisting = false;
				row.newFieldName = MakeUniqueFieldName(go.name, claimedNewNames);
			}

			s_rows[key] = row;
			return row;
		}

		private static void DrawRow(RowState row, GameObject go, List<ExistingFieldInfo> unassignedFields, HashSet<string> claimedExistingFields)
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(go.name, EditorStyles.boldLabel, GUILayout.Width(140));

				if (row.candidateTypes.Count > 1)
				{
					string[] names = row.candidateTypes.Select(UIComponentTypeCatalog.FriendlyTypeName).ToArray();
					row.selectedTypeIndex = EditorGUILayout.Popup(Mathf.Clamp(row.selectedTypeIndex, 0, names.Length - 1), names);
				}
				else
				{
					row.selectedTypeIndex = 0;
					EditorGUILayout.LabelField(UIComponentTypeCatalog.FriendlyTypeName(row.candidateTypes[0]), GUILayout.Width(100));
				}
				EditorGUILayout.EndHorizontal();

				var matchingExisting = unassignedFields
					.Where(f => (f.fieldName == row.existingFieldName || !claimedExistingFields.Contains(f.fieldName))
						&& UIComponentTypeCatalog.HasReference(go, f.fieldType))
					.Select(f => f.fieldName)
					.ToList();

				int mode = matchingExisting.Count > 0
					? GUILayout.Toolbar(row.assignExisting ? 0 : 1, new[] { "Existing Field", "New Field" })
					: 1;
				row.assignExisting = mode == 0;

				if (row.assignExisting)
				{
					int idx = Mathf.Max(0, matchingExisting.IndexOf(row.existingFieldName));
					idx = EditorGUILayout.Popup("Field", idx, matchingExisting.ToArray());
					row.existingFieldName = matchingExisting[idx];
				}
				else
				{
					row.newFieldName = EditorGUILayout.TextField("New field name", row.newFieldName);
				}
			}
		}

		private static string MakeUniqueFieldName(string gameObjectName, HashSet<string> reserved)
		{
			string baseName = ToCamelIdentifier(gameObjectName);
			string candidate = baseName;
			int suffix = 1;
			while (reserved.Contains(candidate))
			{
				suffix++;
				candidate = baseName + suffix;
			}
			return candidate;
		}

		private static string ToCamelIdentifier(string name)
		{
			if (string.IsNullOrEmpty(name))
				return "field";

			var sb = new StringBuilder();
			bool capitalizeNext = false;
			foreach (char c in name)
			{
				if (char.IsLetterOrDigit(c))
				{
					sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
					capitalizeNext = false;
				}
				else
				{
					capitalizeNext = sb.Length > 0;
				}
			}

			if (sb.Length == 0)
				return "field";

			string id = sb.ToString();
			if (char.IsDigit(id[0]))
				id = "_" + id;
			return char.ToLowerInvariant(id[0]) + id.Substring(1);
		}

		private static void Commit(SerializedProperty property, MonoBehaviour view, string path, string scriptPath, List<RowState> rows)
		{
			var immediateRows = rows.Where(r => r.assignExisting && !string.IsNullOrEmpty(r.existingFieldName)).ToList();
			var newFieldRows = rows.Where(r => !r.assignExisting && !string.IsNullOrEmpty(r.newFieldName)).ToList();

			if (immediateRows.Count > 0)
			{
				var so = property.serializedObject;
				foreach (var row in immediateRows)
				{
					var prop = so.FindProperty(row.existingFieldName);
					if (prop == null)
						continue;
					Type selectedType = row.candidateTypes[row.selectedTypeIndex];
					prop.objectReferenceValue = UIComponentTypeCatalog.ResolveReference(row.gameObject, selectedType);
				}
				so.ApplyModifiedProperties();
			}

			if (newFieldRows.Count > 0)
			{
				var specs = new List<NewFieldSpec>();
				var pending = new List<(string fieldName, UnityEngine.Object component)>();
				foreach (var row in newFieldRows)
				{
					Type selectedType = row.candidateTypes[row.selectedTypeIndex];
					specs.Add(new NewFieldSpec(UIComponentTypeCatalog.QualifiedTypeName(selectedType), row.newFieldName));
					pending.Add((row.newFieldName, UIComponentTypeCatalog.ResolveReference(row.gameObject, selectedType)));
				}

				if (ScriptFieldWriter.TryInsertFields(scriptPath, specs, out string error))
				{
					PendingFieldAttach.Write(view, pending);
					AssetDatabase.Refresh();
				}
				else
				{
					Debug.LogError($"[UIWidgets] UIRefBinder failed to insert fields into '{scriptPath}': {error}");
				}
			}

			foreach (var row in rows)
				s_rows.Remove((view, path, row.gameObject));
		}
	}
}
#endif
