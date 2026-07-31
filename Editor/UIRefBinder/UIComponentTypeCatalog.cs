#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.UIWidgets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	/// <summary>Known UI component types considered for binding, in priority order (most specific/common first).</summary>
	internal static class UIComponentTypeCatalog
	{
		internal static readonly Type[] KnownTypes =
		{
			typeof(Button),
			typeof(ButtonX),
			typeof(TMP_Dropdown),
			typeof(TMP_InputField),
			typeof(TextMeshProUGUI),
			typeof(Dropdown),
			typeof(InputField),
			typeof(Text),
			typeof(Toggle),
			typeof(Slider),
			typeof(ScrollRect),
			typeof(Image),
		};

		private static readonly Dictionary<string, Type> KnownTypesByShortName = BuildShortNameLookup();

		private static Dictionary<string, Type> BuildShortNameLookup()
		{
			var map = new Dictionary<string, Type>(StringComparer.Ordinal);
			foreach (var t in KnownTypes)
				map[t.Name] = t;
			return map;
		}

		/// <summary>Known types present as components directly on this GameObject, most-specific first.
		/// Falls back to the GameObject itself when none of the known UI component types are present.</summary>
		internal static List<Type> GetCandidateTypes(GameObject go)
		{
			var result = new List<Type>();
			foreach (var t in KnownTypes)
			{
				if (go.GetComponent(t) != null)
					result.Add(t);
			}

			if (result.Count == 0)
				result.Add(typeof(GameObject));

			return result;
		}

		/// <summary>Resolves the reference a candidate type stands for: the GameObject itself for the
		/// <see cref="GameObject"/> fallback, or the matching component otherwise.</summary>
		internal static UnityEngine.Object ResolveReference(GameObject go, Type type) =>
			type == typeof(GameObject) ? go : go.GetComponent(type);

		/// <summary>Whether this GameObject can provide a reference of the given field type (a component
		/// of that type, or the GameObject itself when the field type is <see cref="GameObject"/>).</summary>
		internal static bool HasReference(GameObject go, Type type) =>
			type == typeof(GameObject) || go.GetComponent(type) != null;

		/// <summary>Resolves an unqualified type name (as written in hand-authored source) to a Type,
		/// checking the known catalog first, then falling back to a project-wide search by short name.</summary>
		internal static bool TryResolveShortName(string typeName, out Type type)
		{
			if (KnownTypesByShortName.TryGetValue(typeName, out type))
				return true;

			type = Type.GetType(typeName);
			if (type != null)
				return true;

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = asm.GetTypes(); }
				catch { continue; }

				foreach (var t in types)
				{
					if (t != null && t.Name == typeName)
					{
						type = t;
						return true;
					}
				}
			}

			type = null;
			return false;
		}

		/// <summary>The type name as it should appear in a generated field declaration (fully qualified,
		/// so the inserted line compiles regardless of the target script's existing using directives).</summary>
		internal static string QualifiedTypeName(Type type) => type.FullName;

		internal static string FriendlyTypeName(Type type)
		{
			const string uiPrefix = "UnityEngine.UI.";
			string full = type.FullName ?? type.Name;
			return full.StartsWith(uiPrefix, StringComparison.Ordinal) ? full.Substring(uiPrefix.Length) : type.Name;
		}
	}
}
#endif
