#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	/// <summary>
	/// Adds a "Bind UI From Selection" foldout to every MonoBehaviour's Inspector via the shared
	/// AetherInspectorEditor extension hook. Lets a designer lock the Inspector on their hand-written
	/// view script, multi-select UI elements in the Hierarchy, and wire/declare fields for them without
	/// ever leaving that Inspector.
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

		private static bool s_expanded;
		private static readonly Dictionary<GameObject, RowState> s_rows = new Dictionary<GameObject, RowState>();

		[InitializeOnLoadMethod]
		private static void Register()
		{
			AetherInspectorEditor.DrawExtraSections += OnDrawExtraSections;
		}

		private static void OnDrawExtraSections(UnityEditor.Editor editor)
		{
			if (editor.targets.Length != 1 || editor.target is not MonoBehaviour view)
				return;

			MonoScript script = MonoScript.FromMonoBehaviour(view);
			if (script == null)
				return;
			string scriptPath = AssetDatabase.GetAssetPath(script);
			if (string.IsNullOrEmpty(scriptPath))
				return;

			EditorGUILayout.Space();
			s_expanded = EditorGUILayout.Foldout(s_expanded, "Bind UI From Selection", true);
			if (!s_expanded)
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

				string scriptSource;
				try { scriptSource = File.ReadAllText(Path.GetFullPath(scriptPath)); }
				catch (Exception ex)
				{
					EditorGUILayout.HelpBox($"Could not read script source: {ex.Message}", MessageType.Error);
					return;
				}

				List<ExistingFieldInfo> existingFields = ScriptFieldScanner.ScanFields(view, scriptSource);
				var unassignedFields = existingFields.Where(f => !f.isAssigned).ToList();

				PruneStaleRows(selected);

				var claimedExistingFields = new HashSet<string>();
				var claimedNewNames = new HashSet<string>(existingFields.Select(f => f.fieldName));
				var rowsInOrder = new List<RowState>();

				foreach (var go in selected)
				{
					RowState row = GetOrCreateRow(go, unassignedFields, claimedExistingFields, claimedNewNames);
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
						Commit(view, scriptPath, rowsInOrder);
					}
				}
			}
			finally
			{
				EditorGUI.indentLevel--;
			}
		}

		private static void PruneStaleRows(List<GameObject> selected)
		{
			if (s_rows.Count == 0)
				return;

			var selectedSet = new HashSet<GameObject>(selected);
			var stale = s_rows.Keys.Where(go => go == null || !selectedSet.Contains(go)).ToList();
			foreach (var go in stale)
				s_rows.Remove(go);
		}

		private static RowState GetOrCreateRow(GameObject go, List<ExistingFieldInfo> unassignedFields,
			HashSet<string> claimedExistingFields, HashSet<string> claimedNewNames)
		{
			if (s_rows.TryGetValue(go, out var existing))
			{
				existing.candidateTypes = UIComponentTypeCatalog.GetCandidateTypes(go);
				return existing;
			}

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

			s_rows[go] = row;
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

		private static void Commit(MonoBehaviour view, string scriptPath, List<RowState> rows)
		{
			var immediateRows = rows.Where(r => r.assignExisting && !string.IsNullOrEmpty(r.existingFieldName)).ToList();
			var newFieldRows = rows.Where(r => !r.assignExisting && !string.IsNullOrEmpty(r.newFieldName)).ToList();

			if (immediateRows.Count > 0)
			{
				var so = new SerializedObject(view);
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
				s_rows.Remove(row.gameObject);
		}
	}
}
#endif
