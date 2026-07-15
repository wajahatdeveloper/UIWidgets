#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="UITabs"/>.</summary>
	public class TabsTest : MonoBehaviour
	{
		[Tooltip("Assign or let DemoGalleryBootstrap spawn Demo_Tabs.")]
		public UITabs tabs;

		private void OnEnable()
		{
			if (tabs == null)
				tabs = FindFirstObjectByType<UITabs>();
		}

		[ContextMenu("Select Tab 0")]
		public void TestTab0() => Select(0);

		[ContextMenu("Select Tab 1")]
		public void TestTab1() => Select(1);

		[ContextMenu("Rebuild")]
		public void TestRebuild()
		{
			if (tabs == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign UITabs (or run Ensure Gallery).");
				return;
			}

			tabs.RebuildTabsFromParents();
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] UITabs rebuilt.");
		}

		private void Select(int index)
		{
			if (tabs == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign UITabs (or run Ensure Gallery).");
				return;
			}

			tabs.OnClick_TabButton(index);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] UITabs select {Index}.", index);
		}

		private void OnGUI()
		{
			if (!UIWidgetsDemoImgui.IsSection(UIWidgetsDemoImgui.Section.Lists))
				return;

			float y = UIWidgetsDemoImgui.ContentY + 110f;
			UIWidgetsDemoImgui.Label(ref y, tabs != null ? tabs.name : "no Tabs");
			if (UIWidgetsDemoImgui.Button(ref y, "Tab 0")) TestTab0();
			if (UIWidgetsDemoImgui.Button(ref y, "Tab 1")) TestTab1();
			if (UIWidgetsDemoImgui.Button(ref y, "Tab Rebuild")) TestRebuild();
		}
	}
}
#endif
