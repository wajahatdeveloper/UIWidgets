#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="ContextMenuWidget"/>.</summary>
	public class ContextMenuTest : MonoBehaviour, IDemoImguiHarness
	{
		[ContextMenu("Show At Center")]
		public void TestShowCenter()
		{
			if (!ContextMenuWidget.HasInstance)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] ContextMenuWidget missing — spawn Singletons/ContextMenu.");
				return;
			}

			var actions = new List<ContextMenuActionData>
			{
				new ContextMenuActionData("copy", "Copy", true, true,
					() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ContextMenu Copy.")),
				new ContextMenuActionData("paste", "Paste", true, true,
					() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ContextMenu Paste.")),
				new ContextMenuActionData("disabled", "Disabled", false, true, () => { }),
			};

			var placement = new ContextMenuPlacementOptions(
				ContextMenuPlacementMode.ScreenPosition,
				new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
				null,
				ContextMenuPreferredDirection.Auto,
				Vector2.zero,
				true);

			ContextMenuWidget.Show(new ContextMenuRequest(actions, placement, true, true, true, this));
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ContextMenu shown.");
		}

		[ContextMenu("Hide All")]
		public void TestHide() => ContextMenuWidget.HideAll();

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, ContextMenuWidget.HasInstance ? "CtxMenu ok" : "need CtxMenu");
			if (UIWidgetsDemoImgui.Button(ref y, "Ctx Show")) TestShowCenter();
			if (UIWidgetsDemoImgui.Button(ref y, "Ctx Hide")) TestHide();
		}
	}
}
#endif
