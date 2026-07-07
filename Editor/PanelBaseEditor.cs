#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIWidgets.Editor
{
	[CustomEditor(typeof(PanelBase), true)]
	[CanEditMultipleObjects]
	public class PanelBaseEditor : UnityEditor.Editor
	{
		private const string FallbackOutputFolder = "Assets";
		private const string PreferredPanelsOutputFolder = "Assets/Scripts/UI/Panels";
		private const string PendingComponentKey = "UIWidgets.PanelBaseEditor.PendingComponentGlobalId";
		private const string PendingScriptPathKey = "UIWidgets.PanelBaseEditor.PendingScriptPath";
		private const string PendingReplacementKey = "UIWidgets.PanelBaseEditor.PendingReplacement";
		private const string PendingRetryCountKey = "UIWidgets.PanelBaseEditor.PendingRetryCount";
		private const int MaxResolveRetries = 5;

		private static bool _isCompilationHandlerRegistered;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var isModal = serializedObject.FindProperty("isModal");
			var draggable = serializedObject.FindProperty("Draggable");

			// Draw every serialized field except the two mutually-exclusive toggles, which we
			// render below with radio-style behavior.
			DrawPropertiesExcluding(serializedObject, "m_Script", "isModal", "Draggable");

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Behavior Mode", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Modal and Draggable are mutually exclusive. Modal = shown one-at-a-time behind an " +
				"input-blocking backdrop (via ModalService). Draggable = free-floating window. " +
				"Enabling one clears the other.",
				MessageType.None);

			if (isModal != null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(isModal);
				if (EditorGUI.EndChangeCheck() && isModal.boolValue && draggable != null)
				{
					draggable.boolValue = false;
				}
			}

			if (draggable != null)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(draggable);
				if (EditorGUI.EndChangeCheck() && draggable.boolValue && isModal != null)
				{
					isModal.boolValue = false;
				}
			}

			serializedObject.ApplyModifiedProperties();

			// The generator only makes sense on a raw PanelBase (it creates a derived subclass).
			if (target != null && target.GetType() == typeof(PanelBase))
			{
				EditorGUILayout.Space(8);
				EditorGUILayout.LabelField("Script Generation", EditorStyles.boldLabel);
				if (GUILayout.Button("Generate Derived Panel Script"))
				{
					OpenGeneratorWindow((PanelBase)target);
				}
			}
		}

		private void OpenGeneratorWindow(PanelBase panel)
		{
			if (panel == null)
			{
				throw new InvalidOperationException("PanelBaseEditor.OpenGeneratorWindow: target PanelBase is missing.");
			}

			var defaultClassName = EnsureSuffix(SanitizeClassName(panel.gameObject.name), "Panel");
			var defaultFileName = defaultClassName + ".cs";
			var outputFolder = ResolveDefaultOutputFolder();

			ScriptGeneratorWindow.Show(
				"PanelBase Script Generator",
				outputFolder,
				defaultFileName,
				defaultClassName,
				string.Empty,
				BuildCode,
				ValidateInput,
				generatedPath => HandleGenerated(panel, generatedPath));
		}

		private static string BuildCode(ScriptGeneratorWindow.GenerationContext context)
		{
			var name = SanitizeClassName(context.ClassName);
			var namespaceValue = (context.Namespace ?? string.Empty).Trim();
			var sb = new StringBuilder();

			if (!string.IsNullOrEmpty(namespaceValue))
			{
				sb.AppendLine("namespace " + namespaceValue);
				sb.AppendLine("{");
			}

			var indent = string.IsNullOrEmpty(namespaceValue) ? string.Empty : "\t";
			sb.AppendLine(indent + "public class " + name + " : UIWidgets.PanelBase");
			sb.AppendLine(indent + "{");
			sb.AppendLine(indent + "\tprotected override void OnBeforeShow()");
			sb.AppendLine(indent + "\t{");
			sb.AppendLine(indent + "\t\tbase.OnBeforeShow();");
			sb.AppendLine(indent + "\t}");
			sb.AppendLine();
			sb.AppendLine(indent + "\tprotected override void OnAfterShow()");
			sb.AppendLine(indent + "\t{");
			sb.AppendLine(indent + "\t\tbase.OnAfterShow();");
			sb.AppendLine(indent + "\t}");
			sb.AppendLine();
			sb.AppendLine(indent + "\tprotected override void OnBeforeHide()");
			sb.AppendLine(indent + "\t{");
			sb.AppendLine(indent + "\t\tbase.OnBeforeHide();");
			sb.AppendLine(indent + "\t}");
			sb.AppendLine();
			sb.AppendLine(indent + "\tprotected override void OnAfterHide()");
			sb.AppendLine(indent + "\t{");
			sb.AppendLine(indent + "\t\tbase.OnAfterHide();");
			sb.AppendLine(indent + "\t}");
			sb.AppendLine(indent + "}");

			if (!string.IsNullOrEmpty(namespaceValue))
			{
				sb.AppendLine("}");
			}

			return sb.ToString();
		}

		private static string ValidateInput(ScriptGeneratorWindow.GenerationContext context)
		{
			var sanitizedClassName = SanitizeClassName(context.ClassName);
			if (string.IsNullOrEmpty(sanitizedClassName))
			{
				return "Class name is required.";
			}

			if (!string.Equals(sanitizedClassName, context.ClassName, StringComparison.Ordinal))
			{
				return "Class name contains invalid characters.";
			}

			if (string.IsNullOrWhiteSpace(context.Namespace))
			{
				return null;
			}

			var ns = context.Namespace.Trim();
			if (!IsValidNamespace(ns))
			{
				return "Namespace is not a valid C# namespace.";
			}

			return null;
		}

		private void HandleGenerated(PanelBase panel, string generatedPath)
		{
			if (panel == null)
			{
				throw new InvalidOperationException("PanelBaseEditor.HandleGenerated: source PanelBase is missing.");
			}

			var globalId = GlobalObjectId.GetGlobalObjectIdSlow(panel).ToString();
			SessionState.SetString(PendingComponentKey, globalId);
			SessionState.SetString(PendingScriptPathKey, generatedPath);
			SessionState.SetBool(PendingReplacementKey, true);

			if (!_isCompilationHandlerRegistered)
			{
				CompilationPipeline.compilationFinished += OnCompilationFinished;
				_isCompilationHandlerRegistered = true;
			}
		}

		private static void OnCompilationFinished(object _)
		{
			TryReplacePendingPanel();
		}

		[InitializeOnLoadMethod]
		private static void InitializeOnLoad()
		{
			EditorApplication.delayCall += TryReplacePendingPanel;
		}

		private static void TryReplacePendingPanel()
		{
			if (!SessionState.GetBool(PendingReplacementKey, false))
			{
				return;
			}

			var componentGlobalId = SessionState.GetString(PendingComponentKey, string.Empty);
			var scriptPath = SessionState.GetString(PendingScriptPathKey, string.Empty);
			if (string.IsNullOrEmpty(componentGlobalId) || string.IsNullOrEmpty(scriptPath))
			{
				ClearPendingReplacement();
				throw new InvalidOperationException("PanelBase replacement state is incomplete.");
			}

			if (!GlobalObjectId.TryParse(componentGlobalId, out var parsedComponentId))
			{
				ClearPendingReplacement();
				throw new InvalidOperationException("Failed parsing pending PanelBase global object id.");
			}

			var sourceComponent = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsedComponentId) as PanelBase;
			if (sourceComponent == null)
			{
				ClearPendingReplacement();
				throw new InvalidOperationException("Original PanelBase component could not be resolved.");
			}

			if (!File.Exists(scriptPath))
			{
				ClearPendingReplacement();
				throw new FileNotFoundException("Generated script file not found.", scriptPath);
			}

			var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
			var scriptType = script != null ? script.GetClass() : null;
			if (scriptType == null)
			{
				// The generated script may not have compiled yet. Retry a bounded number of
				// times; if it never resolves (e.g. a compile error in the generated file)
				// clear the pending state so we don't re-enter indefinitely on every domain load.
				var retryCount = SessionState.GetInt(PendingRetryCountKey, 0);
				if (retryCount >= MaxResolveRetries)
				{
					ClearPendingReplacement();
					throw new InvalidOperationException(
						$"Generated script at '{scriptPath}' could not be resolved to a type after {MaxResolveRetries} attempts.");
				}

				SessionState.SetInt(PendingRetryCountKey, retryCount + 1);
				return;
			}

			if (!typeof(PanelBase).IsAssignableFrom(scriptType))
			{
				ClearPendingReplacement();
				throw new InvalidOperationException($"Generated script type '{scriptType.FullName}' does not inherit from PanelBase.");
			}

			ReplaceComponent(sourceComponent, scriptType);
			ClearPendingReplacement();
		}

		private static void ReplaceComponent(PanelBase sourceComponent, Type destinationType)
		{
			if (sourceComponent == null)
			{
				throw new InvalidOperationException("Cannot replace null PanelBase component.");
			}

			var owner = sourceComponent.gameObject;
			if (owner == null)
			{
				throw new InvalidOperationException("PanelBase GameObject is missing.");
			}

			Undo.IncrementCurrentGroup();
			var groupId = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Replace PanelBase With Generated Script");
			Undo.RegisterCompleteObjectUndo(owner, "Replace PanelBase With Generated Script");

			var json = EditorJsonUtility.ToJson(sourceComponent);
			var newComponent = Undo.AddComponent(owner, destinationType) as PanelBase;
			if (newComponent == null)
			{
				Undo.CollapseUndoOperations(groupId);
				throw new InvalidOperationException($"Failed to add generated component '{destinationType.FullName}'.");
			}

			EditorJsonUtility.FromJsonOverwrite(json, newComponent);
			Undo.DestroyObjectImmediate(sourceComponent);
			EditorSceneManager.MarkSceneDirty(owner.scene);
			Selection.activeObject = newComponent;
			EditorGUIUtility.PingObject(newComponent);
			Undo.CollapseUndoOperations(groupId);
		}

		private static string SanitizeClassName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return "New";
			}

			var sb = new StringBuilder(value.Length);
			for (var i = 0; i < value.Length; i++)
			{
				var c = value[i];
				if (i == 0)
				{
					if (char.IsLetter(c) || c == '_')
					{
						sb.Append(c);
					}
				}
				else if (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c);
				}
			}

			if (sb.Length == 0)
			{
				return "New";
			}

			return sb.ToString();
		}

		private static string EnsureSuffix(string value, string suffix)
		{
			if (string.IsNullOrEmpty(value))
			{
				return suffix;
			}

			return value.EndsWith(suffix, StringComparison.Ordinal) ? value : value + suffix;
		}

		private static string ResolveDefaultOutputFolder()
		{
			return AssetDatabase.IsValidFolder(PreferredPanelsOutputFolder) ? PreferredPanelsOutputFolder : FallbackOutputFolder;
		}

		private static bool IsValidNamespace(string namespaceValue)
		{
			var segments = namespaceValue.Split('.');
			for (var i = 0; i < segments.Length; i++)
			{
				var part = segments[i];
				if (string.IsNullOrWhiteSpace(part))
				{
					return false;
				}

				if (!(char.IsLetter(part[0]) || part[0] == '_'))
				{
					return false;
				}

				for (var c = 1; c < part.Length; c++)
				{
					var ch = part[c];
					if (!(char.IsLetterOrDigit(ch) || ch == '_'))
					{
						return false;
					}
				}
			}

			return true;
		}

		private static void ClearPendingReplacement()
		{
			SessionState.EraseString(PendingComponentKey);
			SessionState.EraseString(PendingScriptPathKey);
			SessionState.EraseInt(PendingRetryCountKey);
			SessionState.SetBool(PendingReplacementKey, false);

			if (_isCompilationHandlerRegistered)
			{
				CompilationPipeline.compilationFinished -= OnCompilationFinished;
				_isCompilationHandlerRegistered = false;
			}
		}
	}
}
#endif
