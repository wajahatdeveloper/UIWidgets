#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	internal readonly struct DeclaredFieldInfo
	{
		public readonly string fieldName;
		public readonly Type fieldType;

		public DeclaredFieldInfo(string fieldName, Type fieldType)
		{
			this.fieldName = fieldName;
			this.fieldType = fieldType;
		}
	}

	internal readonly struct ExistingFieldInfo
	{
		public readonly string fieldName;
		public readonly Type fieldType;
		public readonly UnityEngine.Object currentValue;

		public bool isAssigned => currentValue != null;

		public ExistingFieldInfo(string fieldName, Type fieldType, UnityEngine.Object currentValue)
		{
			this.fieldName = fieldName;
			this.fieldType = fieldType;
			this.currentValue = currentValue;
		}
	}

	/// <summary>
	/// Source-text scan (not Roslyn, matching this codebase's established convention — see
	/// DeterministicRandomLint.cs) for [SerializeField] field declarations of Component-derived types.
	/// Split in two: parsing the raw text (the expensive half — the caller caches this by the script's
	/// last-write time) and cross-referencing against the live SerializedObject for assignment state
	/// (cheap, always re-checked so a just-applied bind is reflected immediately).
	/// </summary>
	internal static class ScriptFieldScanner
	{
		private static readonly Regex FieldRegex = new Regex(
			@"\[SerializeField\]\s*(?:private\s+|protected\s+|public\s+|internal\s+)?" +
			@"(?<type>[A-Za-z_][A-Za-z0-9_.]*)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^;]+)?;",
			RegexOptions.Compiled);

		/// <summary>All Component-typed [SerializeField] fields declared in the script text.</summary>
		internal static List<DeclaredFieldInfo> ParseDeclaredFields(string scriptSource)
		{
			var result = new List<DeclaredFieldInfo>();
			if (string.IsNullOrEmpty(scriptSource))
				return result;

			foreach (Match m in FieldRegex.Matches(scriptSource))
			{
				string typeName = m.Groups["type"].Value;
				string fieldName = m.Groups["name"].Value;

				if (!UIComponentTypeCatalog.TryResolveShortName(typeName, out Type fieldType))
					continue;
				if (!typeof(Component).IsAssignableFrom(fieldType) && fieldType != typeof(GameObject))
					continue;

				result.Add(new DeclaredFieldInfo(fieldName, fieldType));
			}

			return result;
		}

		/// <summary>Cross-references parsed field declarations against the live SerializedObject for current assignment state.</summary>
		internal static List<ExistingFieldInfo> ToExistingFields(SerializedObject so, List<DeclaredFieldInfo> declaredFields)
		{
			var result = new List<ExistingFieldInfo>(declaredFields.Count);
			foreach (var d in declaredFields)
			{
				var prop = so.FindProperty(d.fieldName);
				if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				result.Add(new ExistingFieldInfo(d.fieldName, d.fieldType, prop.objectReferenceValue));
			}

			return result;
		}
	}
}
#endif
