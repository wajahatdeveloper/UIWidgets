#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.Utilities.Menus;
using AetherNexus.GameEngineCore.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Contributes UIWidgets validation status and a Tool Window shortcut to CentralAuthoring
	/// without GameEngineCore.Editor referencing the UIWidgets assembly. Widget placement is a
	/// GameObject/Prefab-drop task done via the UI Widgets Window itself, not a Central Window
	/// workflow.
	/// </summary>
	public sealed class UIWidgetsCentralAuthoringPlugin : ICentralAuthoringPlugin
	{
		public string PackageId => "UIWidgets";
		public string DisplayName => "UI Widgets";
		public int Priority => 100;

		public IReadOnlyList<(string Label, string MenuPath)> GetToolWindowShortcuts()
		{
			return new[]
			{
				("UI Widgets Window", MenuPaths.UIWidgets.WidgetsWindow)
			};
		}

		public void AppendStatusLines(PackageIntegrationManifest manifest, List<PackageTaskStatusLine> lines)
		{
			var scene = SceneManager.GetActiveScene();
			if (!scene.IsValid())
			{
				return;
			}

			var roots = scene.GetRootGameObjects();
			var widgetCount = 0;
			var hasSafeArea = false;
			for (var i = 0; i < roots.Length; i++)
			{
				var behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
				for (var b = 0; b < behaviours.Length; b++)
				{
					var behaviour = behaviours[b];
					if (behaviour == null)
					{
						continue;
					}

					var ns = behaviour.GetType().Namespace;
					if (ns != null && ns.StartsWith("UIWidgets", StringComparison.Ordinal))
					{
						widgetCount++;
						if (behaviour.GetType().Name == "SafeArea")
						{
							hasSafeArea = true;
						}
					}
				}
			}

			lines.Add(new PackageTaskStatusLine
			{
				Message = $"Scene widgets: {widgetCount}",
				Severity = widgetCount > 0 ? PackageTaskStatusSeverity.Info : PackageTaskStatusSeverity.Warning
			});

			if (!hasSafeArea && widgetCount > 0)
			{
				lines.Add(new PackageTaskStatusLine
				{
					Message = "SafeArea missing on active canvas",
					Severity = PackageTaskStatusSeverity.Warning
				});
			}
		}

		public IReadOnlyList<string> GetRegistryContractTypeNames()
		{
			return Array.Empty<string>();
		}
	}
}
#endif