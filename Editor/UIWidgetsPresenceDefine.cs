#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Announces UIWidgets' presence by ensuring the <c>AETHERNEXUS_UIWIDGETS</c>
	/// scripting define symbol is set. Optional consumers (e.g. EditorEnhancerX UI nudge)
	/// gate on this via <c>#if AETHERNEXUS_UIWIDGETS</c>, so they compile when UIWidgets
	/// is installed and stay out when the package is absent.
	///
	/// Applies to every known <see cref="BuildTargetGroup"/> (not only the active one).
	/// Editor assemblies compile against the active target's defines — omitting Standalone
	/// left UI nudge compiled out during normal PC editor work.
	/// </summary>
	[InitializeOnLoad]
	internal static class UIWidgetsPresenceDefine
	{
		private const string Symbol = "AETHERNEXUS_UIWIDGETS";

		static UIWidgetsPresenceDefine()
		{
			EditorApplication.delayCall += EnsureSymbolOnAllTargets;
		}

		private static void EnsureSymbolOnAllTargets()
		{
			foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (group == BuildTargetGroup.Unknown)
					continue;

				try
				{
					EnsureSymbol(NamedBuildTarget.FromBuildTargetGroup(group));
				}
				catch
				{
					// Unknown / deprecated / unsupported groups — skip
				}
			}
		}

		private static void EnsureSymbol(NamedBuildTarget namedTarget)
		{
			string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
			var list = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
			if (list.Contains(Symbol))
				return;

			list.Add(Symbol);
			PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", list));
		}
	}
}
#endif
