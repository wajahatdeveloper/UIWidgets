#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="ScrollList"/> + <see cref="ScrollListItemData"/>.</summary>
	public class ScrollListTest : MonoBehaviour, IDemoImguiHarness
	{
		[Tooltip("Assign or let DemoGalleryBootstrap spawn Demo_ScrollList.")]
		public ScrollList scrollList;

		private void OnEnable()
		{
			if (scrollList == null)
				scrollList = FindFirstObjectByType<ScrollList>();
		}

		[ContextMenu("Bind Sample Data")]
		public void TestBind()
		{
			if (scrollList == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign ScrollList (or run Ensure Gallery).");
				return;
			}

			var items = new List<ScrollListItemData>
			{
				new ScrollListItemData("Alpha", "First row"),
				new ScrollListItemData("Beta", "Second row"),
				new ScrollListItemData("Gamma", "Third row"),
				new ScrollListItemData("Delta", "Fourth row"),
			};
			scrollList.SetDataSource(items);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ScrollList bound {Count} items.", items.Count);
		}

		[ContextMenu("Filter Alpha")]
		public void TestFilter()
		{
			if (scrollList == null)
				return;
			scrollList.FilterItems("Alpha");
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ScrollList filter=Alpha.");
		}

		[ContextMenu("Clear Filter")]
		public void TestClearFilter()
		{
			if (scrollList == null)
				return;
			scrollList.ClearFilter();
		}

		[ContextMenu("Clear All")]
		public void TestClear()
		{
			if (scrollList == null)
				return;
			scrollList.ClearAllItems();
		}

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, scrollList != null ? scrollList.name : "no ScrollList");
			if (UIWidgetsDemoImgui.Button(ref y, "SL Bind")) TestBind();
			if (UIWidgetsDemoImgui.Button(ref y, "SL Filter")) TestFilter();
			if (UIWidgetsDemoImgui.Button(ref y, "SL ClearFlt")) TestClearFilter();
			if (UIWidgetsDemoImgui.Button(ref y, "SL Clear")) TestClear();
		}
	}
}
#endif
