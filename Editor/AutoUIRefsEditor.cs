using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AetherNexus.FoundationPlatform.Editor.Windows;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	[Serializable]
	internal class AutoUIRefsAttachField
	{
		public string fieldName;
		public string componentId;
	}

	[Serializable]
	internal class AutoUIRefsAttachPayload
	{
		public string gameObjectId;
		public string scriptPath;
		public AutoUIRefsAttachField[] fields;
	}
	public static class AutoUIRefsAttachPending
	{
		public static string PendingPath => Path.Combine(Application.persistentDataPath, "AutoUIRefsAttachPending.json");

		internal static bool ProcessPending(string path)
		{
			bool attached = false;
			try
			{
				if (!File.Exists(path))
					return false;
				string json = File.ReadAllText(path);
				var payload = JsonUtility.FromJson<AutoUIRefsAttachPayload>(json);
				if (payload == null || string.IsNullOrEmpty(payload.gameObjectId) || string.IsNullOrEmpty(payload.scriptPath))
					return false;
				if (!GlobalObjectId.TryParse(payload.gameObjectId, out GlobalObjectId gid))
					return false;
				UnityEngine.Object goObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
				if (goObj is not GameObject gameObject)
					return false;
				MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(payload.scriptPath);
				if (monoScript == null)
					return false;
				Type type = monoScript.GetClass();
				if (type == null)
					return false;
				Component addedComponent = gameObject.GetComponent(type);
				if (addedComponent == null)
					addedComponent = Undo.AddComponent(gameObject, type);
				SerializedObject so = new SerializedObject(addedComponent);
				if (payload.fields != null)
				{
					foreach (var entry in payload.fields)
					{
						if (string.IsNullOrEmpty(entry.fieldName) || string.IsNullOrEmpty(entry.componentId))
							continue;
						if (!GlobalObjectId.TryParse(entry.componentId, out GlobalObjectId cid))
							continue;
						UnityEngine.Object compObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(cid);
						if (compObj == null)
							continue;
						var prop = so.FindProperty(entry.fieldName);
						if (prop != null)
							prop.objectReferenceValue = compObj;
					}
					so.ApplyModifiedProperties();
				}
				var autoUIRefs = gameObject.GetComponent<AutoUIRefs>();
				if (autoUIRefs != null)
				{
					var refsSo = new SerializedObject(autoUIRefs);
					var scriptNameProp = refsSo.FindProperty("scriptName");
					if (scriptNameProp != null)
					{
						scriptNameProp.stringValue = gameObject.name + "UIComponents";
						refsSo.ApplyModifiedProperties();
					}
				}
				attached = true;
			}
			finally
			{
				if (attached && File.Exists(path))
					File.Delete(path);
			}
			return attached;
		}

		private const int MaxPendingAttempts = 600;
		private static int pendingAttempts;

		[InitializeOnLoadMethod]
		private static void RegisterProcessPending()
		{
			pendingAttempts = 0;
			EditorApplication.update += PollForPendingFile;
		}

		private static void PollForPendingFile()
		{
			string path = PendingPath;
			if (!File.Exists(path))
			{
				pendingAttempts = 0;
				return;
			}
			if (ProcessPending(path))
			{
				pendingAttempts = 0;
				EditorApplication.update -= PollForPendingFile;
				return;
			}
			pendingAttempts++;
			if (pendingAttempts >= MaxPendingAttempts)
			{
				EditorApplication.update -= PollForPendingFile;
				string failedPath = path + ".failed";
				try
				{
					if (File.Exists(failedPath))
						File.Delete(failedPath);
					File.Move(path, failedPath);
				}
				catch
				{
					// If renaming fails, fall back to deleting so the poll cannot resurrect it.
					try { File.Delete(path); } catch { }
				}
				UnityEngine.Debug.LogWarning(
					$"AutoUIRefs: gave up processing pending attach file after {MaxPendingAttempts} attempts. " +
					$"Renamed to '{failedPath}'.");
			}
		}
	}

	[CustomEditor(typeof(AutoUIRefs))]
	public class AutoUIRefsEditor : UnityEditor.Editor
	{
		private AutoUIRefs uiManager;
		
		// Foldout states for collapsible sections
		private bool showButtons = true;
		private bool showTexts = true;
		private bool showOther = true;

		private void OnEnable()
		{
			uiManager = (AutoUIRefs)target;
		}

		private bool GetSectionFoldout(string section)
		{
			if (section == "Buttons") return showButtons;
			if (section == "Texts") return showTexts;
			return showOther;
		}

		private void SetSectionFoldout(string section, bool value)
		{
			if (section == "Buttons") showButtons = value;
			else if (section == "Texts") showTexts = value;
			else showOther = value;
		}

		private static int SumSectionCount(AutoUIRefs refs, string section)
		{
			int n = 0;
			for (int i = 0; i < AutoUIRefs.UIListDescriptors.Length; i++)
			{
				if (AutoUIRefs.UIListDescriptors[i].section == section)
				{
					var list = refs.GetListAt(i);
					if (list != null) n += list.Count;
				}
			}
			return n;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			uiManager = (AutoUIRefs)target;

			// Header
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Auto UI References", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// Script name
			EditorGUILayout.PropertyField(serializedObject.FindProperty("scriptName"));
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Scan Options", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("includeInactive"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("verboseLogging"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("preventDuplicates"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("includeMonoBehaviourScripts"));

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Visual feedback: Summary
			int totalCount = uiManager.GetTotalCount();

			if (totalCount == 0)
			{
				EditorGUILayout.HelpBox("No UI elements found. Click 'Find UI Elements' to scan the hierarchy.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox($"Found {totalCount} UI element(s)", MessageType.Info);
			}

			EditorGUILayout.Space();

			// Action buttons
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("🔍 Find UI Elements", GUILayout.Height(30)))
			{
				uiManager.FindUIElements(uiManager.gameObject);
				EditorUtility.SetDirty(uiManager);
				serializedObject.Update();
			}
			
			if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(30)))
			{
				uiManager.ClearAll();
				EditorUtility.SetDirty(uiManager);
				serializedObject.Update();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			// Results sections with foldouts (grouped by descriptor section)
			if (totalCount > 0)
			{
				EditorGUILayout.LabelField("Found Components", EditorStyles.boldLabel);
				EditorGUILayout.Space();

				string currentSection = null;
				bool showSection = true;
				for (int i = 0; i < AutoUIRefs.UIListDescriptors.Length; i++)
				{
					var d = AutoUIRefs.UIListDescriptors[i];
					IList list = uiManager.GetListAt(i);
					int count = list?.Count ?? 0;
					if (count == 0) continue;

					if (d.section != currentSection)
					{
						if (currentSection != null)
							EditorGUI.indentLevel--;
						currentSection = d.section;
						int sectionCount = SumSectionCount(uiManager, currentSection);
						showSection = GetSectionFoldout(currentSection);
						showSection = EditorGUILayout.Foldout(showSection, $"{currentSection} ({sectionCount})", true);
						SetSectionFoldout(currentSection, showSection);
						if (showSection)
							EditorGUI.indentLevel++;
					}
					if (showSection)
					{
						var prop = serializedObject.FindProperty(d.propertyName);
						EditorGUILayout.PropertyField(prop, new GUIContent($"{d.typeName} ({count})"), true);
					}
				}
				if (currentSection != null)
					EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			// Generate script buttons
			EditorGUI.BeginDisabledGroup(totalCount == 0);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("📝 Generate Script", GUILayout.Height(25)))
			{
				string folder = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(uiManager))), "Generated");
				string defaultClassName = EnsureSuffix(uiManager.scriptName, "View");
				ScriptGeneratorWindow.Show(
					"Generate UI Refs Script",
					folder,
					defaultClassName + ".cs",
					defaultClassName,
					string.Empty,
					BuildGeneratedCode,
					ValidateForGenerate);
			}
			if (GUILayout.Button("Generate and Attach", GUILayout.Height(25)))
			{
				string folder = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(uiManager))), "Generated");
				string defaultClassName = EnsureSuffix(uiManager.scriptName, "View");
				ScriptGeneratorWindow.Show(
					"Generate UI Refs Script",
					folder,
					defaultClassName + ".cs",
					defaultClassName,
					string.Empty,
					BuildGeneratedCode,
					ValidateForGenerate,
					onGenerated: WriteAttachPending);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			if (totalCount == 0)
			{
				EditorGUILayout.HelpBox("Scan for UI elements first before generating script.", MessageType.None);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private string ValidateForGenerate(ScriptGeneratorWindow.GenerationContext context)
		{
			if (string.IsNullOrWhiteSpace(context.ClassName))
				return "Script name is required.";
			if (context.ClassName.Length > 0 && (char.IsDigit(context.ClassName[0]) || context.ClassName.Any(c => !char.IsLetterOrDigit(c) && c != '_')))
				return "Script name must be a valid C# identifier.";
			if (!string.IsNullOrWhiteSpace(context.Namespace) && !IsValidNamespace(context.Namespace))
				return "Namespace is not a valid C# namespace.";
			if (uiManager.GetTotalCount() == 0)
				return "Scan for UI elements first.";
			return null;
		}

		private static string ToValidIdentifier(string gameObjectName, HashSet<string> used)
		{
			if (string.IsNullOrEmpty(gameObjectName))
				gameObjectName = "Unnamed";
			var sb = new StringBuilder();
			foreach (char c in gameObjectName)
			{
				if (char.IsLetterOrDigit(c) || c == '_')
					sb.Append(c);
				else if (char.IsWhiteSpace(c) || c == '-' || c == '.')
					sb.Append('_');
			}
			if (sb.Length == 0)
				sb.Append("Field");
			else if (char.IsDigit(sb[0]))
				sb.Insert(0, '_');
			string baseId = sb.ToString();
			string id = baseId;
			int suffix = 0;
			while (used.Contains(id))
			{
				suffix++;
				id = baseId + "_" + suffix;
			}
			used.Add(id);
			return id;
		}

		private static string EnsureSuffix(string value, string suffix)
		{
			if (string.IsNullOrWhiteSpace(value))
				return suffix;
			return value.EndsWith(suffix, StringComparison.Ordinal) ? value : value + suffix;
		}

		private string BuildGeneratedCode(ScriptGeneratorWindow.GenerationContext context)
		{
			var used = new HashSet<string>();
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("using System;");
			sb.AppendLine("using TMPro;");
			sb.AppendLine("using UnityEngine;");
			sb.AppendLine("using UnityEngine.UI;");
			sb.AppendLine();
			var namespaceValue = (context.Namespace ?? string.Empty).Trim();
			if (!string.IsNullOrEmpty(namespaceValue))
			{
				sb.AppendLine($"namespace {namespaceValue}");
				sb.AppendLine("{");
			}
			var indent = string.IsNullOrEmpty(namespaceValue) ? string.Empty : "\t";
			sb.AppendLine($"{indent}public class {context.ClassName} : MonoBehaviour");
			sb.AppendLine($"{indent}{{");

			for (int i = 0; i < AutoUIRefs.UIListDescriptors.Length; i++)
			{
				IList list = uiManager.GetListAt(i);
				if (list == null) continue;
				var components = new List<Component>();
				foreach (var o in list)
					components.Add((Component)o);
				AppendFields(sb, used, AutoUIRefs.UIListDescriptors[i], components, indent + "\t");
			}

			sb.AppendLine($"{indent}}}");
			if (!string.IsNullOrEmpty(namespaceValue))
			{
				sb.AppendLine("}");
			}
			return sb.ToString();
		}

		private void AppendFields(StringBuilder sb, HashSet<string> used, AutoUIRefs.UIListDescriptor descriptor, List<Component> components, string indent)
		{
			foreach (Component component in components)
			{
				string id = ToValidIdentifier(component.gameObject.name, used);
				sb.AppendLine($"{indent}public {GetGeneratedFieldTypeName(descriptor, component)} {id};");
			}
		}

		private static bool IsValidNamespace(string namespaceValue)
		{
			var segments = namespaceValue.Split('.');
			for (int i = 0; i < segments.Length; i++)
			{
				var part = segments[i];
				if (string.IsNullOrWhiteSpace(part))
					return false;
				if (!(char.IsLetter(part[0]) || part[0] == '_'))
					return false;
				for (int c = 1; c < part.Length; c++)
				{
					var ch = part[c];
					if (!(char.IsLetterOrDigit(ch) || ch == '_'))
						return false;
				}
			}
			return true;
		}

		private static string GetGeneratedFieldTypeName(AutoUIRefs.UIListDescriptor descriptor, Component component)
		{
			if (descriptor.propertyName != "monoBehaviourScripts")
			{
				return StripUnityUiPrefix(descriptor.typeName);
			}

			Type type = component.GetType();
			string fullName = type.FullName?.Replace('+', '.');
			if (string.IsNullOrWhiteSpace(fullName))
			{
				return "MonoBehaviour";
			}

			return StripUnityUiPrefix(fullName);
		}

		private static string StripUnityUiPrefix(string typeName)
		{
			const string prefix = "UnityEngine.UI.";
			if (!string.IsNullOrWhiteSpace(typeName) && typeName.StartsWith(prefix, StringComparison.Ordinal))
			{
				return typeName.Substring(prefix.Length);
			}
			return typeName;
		}

		private List<(string fieldName, Component component)> BuildOrderedFields()
		{
			var used = new HashSet<string>();
			var list = new List<(string, Component)>();
			for (int i = 0; i < AutoUIRefs.UIListDescriptors.Length; i++)
			{
				IList compList = uiManager.GetListAt(i);
				if (compList == null) continue;
				foreach (var o in compList)
				{
					var c = (Component)o;
					string id = ToValidIdentifier(c.gameObject.name, used);
					list.Add((id, c));
				}
			}
			return list;
		}

		private void WriteAttachPending(string scriptPath)
		{
			var ordered = BuildOrderedFields();
			var payload = new AutoUIRefsAttachPayload
			{
				gameObjectId = GlobalObjectId.GetGlobalObjectIdSlow(uiManager.gameObject).ToString(),
				scriptPath = scriptPath,
				fields = ordered.Select(p => new AutoUIRefsAttachField
				{
					fieldName = p.fieldName,
					componentId = GlobalObjectId.GetGlobalObjectIdSlow(p.component).ToString()
				}).ToArray()
			};
			string path = AutoUIRefsAttachPending.PendingPath;
			File.WriteAllText(path, JsonUtility.ToJson(payload));
		}
	}
}