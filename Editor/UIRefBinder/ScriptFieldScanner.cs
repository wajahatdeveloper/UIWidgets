#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	internal readonly struct ExistingFieldInfo
	{
		public readonly string fieldName;
		public readonly Type fieldType;
		public readonly bool isAssigned;

		public ExistingFieldInfo(string fieldName, Type fieldType, bool isAssigned)
		{
			this.fieldName = fieldName;
			this.fieldType = fieldType;
			this.isAssigned = isAssigned;
		}
	}

	/// <summary>
	/// Source-text scan (not Roslyn, matching this codebase's established convention — see
	/// DeterministicRandomLint.cs) for [SerializeField] field declarations of Component-derived types
	/// in a target script, cross-referenced against the live SerializedObject to know which are assigned.
	/// </summary>
	internal static class ScriptFieldScanner
	{
		private static readonly Regex FieldRegex = new Regex(
			@"\[SerializeField\]\s*(?:private\s+|protected\s+|public\s+|internal\s+)?" +
			@"(?<type>[A-Za-z_][A-Za-z0-9_.]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^;]+)?;",
			RegexOptions.Compiled);

		/// <summary>All Component-typed [SerializeField] fields declared in the script, with their current assignment state.</summary>
		internal static List<ExistingFieldInfo> ScanFields(MonoBehaviour target, string scriptSource)
		{
			var result = new List<ExistingFieldInfo>();
			if (target == null || string.IsNullOrEmpty(scriptSource))
				return result;

			var so = new SerializedObject(target);

			foreach (Match m in FieldRegex.Matches(scriptSource))
			{
				string typeName = m.Groups["type"].Value;
				string fieldName = m.Groups["name"].Value;

				if (!UIComponentTypeCatalog.TryResolveShortName(typeName, out Type fieldType))
					continue;
				if (!typeof(Component).IsAssignableFrom(fieldType) && fieldType != typeof(GameObject))
					continue;

				var prop = so.FindProperty(fieldName);
				if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				result.Add(new ExistingFieldInfo(fieldName, fieldType, prop.objectReferenceValue != null));
			}

			return result;
		}
	}
}
#endif
