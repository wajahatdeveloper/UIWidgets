#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using FoundationPlatform.Utilities.Menus;
using GameEngineCore.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameEngineCore.Editor.PackageWorkflowBuilder;

namespace UIWidgets.Editor
{
	/// <summary>
	/// Contributes a UIWidgets setup workflow to CentralAuthoring without
	/// GameEngineCore.Editor referencing the UIWidgets assembly.
	/// </summary>
	public sealed class UIWidgetsCentralAuthoringPlugin : ICentralAuthoringPlugin
	{
		public string PackageId => "UIWidgets";
		public string DisplayName => "UI Widgets";
		public int Priority => 100;

		public PackageWorkflowDefinition[] GetWorkflowDefinitions()
		{
			return new[]
			{
				new PackageWorkflowDefinition
				{
					WorkflowId = "widget-setup",
					DisplayLabel = "Widget Setup",
					ContextSectionTitle = "UI Widgets Context",
					ContextMessage =
						"Browse the shared widget library and place reusable UI widgets into your scene canvases.",
					SourceOfTruthMessage =
						"Widget prefabs are curated in the UI Widgets library. Open the UI Widgets Window to browse and instantiate them into the open scene (Scene).",
					ScopedTypeNames = Array.Empty<string>(),
					PrimaryActions = new[]
					{
						ActionMenu("Open UI Widgets Window", MenuPaths.UIWidgets.WidgetsWindow)
					},
					ReadinessChecks = Array.Empty<PackageWorkflowReadinessDef>(),
					IntegrationLinks = Array.Empty<PackageWorkflowIntegrationLinkDef>()
				}
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
