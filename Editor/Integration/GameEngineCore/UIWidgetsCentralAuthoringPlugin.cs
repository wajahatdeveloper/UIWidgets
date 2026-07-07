#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameEngineCore.Editor;
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
						ActionMenu("Open UI Widgets Window", "Window/UIWidgets/UI Widgets...")
					},
					ReadinessChecks = Array.Empty<PackageWorkflowReadinessDef>(),
					IntegrationLinks = Array.Empty<PackageWorkflowIntegrationLinkDef>()
				}
			};
		}

		public void AppendStatusLines(PackageIntegrationManifest manifest, List<PackageTaskStatusLine> lines)
		{
		}

		public IReadOnlyList<string> GetTier1ContractTypeNames()
		{
			return Array.Empty<string>();
		}
	}
}
#endif
