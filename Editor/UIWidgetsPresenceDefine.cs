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
	/// Runs for the active build target group on every domain reload; switching platform
	/// re-runs it for the newly selected group. Idempotent — no-op once the symbol is present.
	/// </summary>
	[InitializeOnLoad]
	internal static class UIWidgetsPresenceDefine
	{
		private const string Symbol = "AETHERNEXUS_UIWIDGETS";

		static UIWidgetsPresenceDefine()
		{
			try
			{
				var group = EditorUserBuildSettings.selectedBuildTargetGroup;
				if (group == BuildTargetGroup.Unknown)
					return;

				var namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
				string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

				var list = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
				if (list.Contains(Symbol))
					return;

				list.Add(Symbol);
				PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", list));
			}
			catch
			{
				// best effort — never block editor startup on define bookkeeping
			}
		}
	}
}
#endif
