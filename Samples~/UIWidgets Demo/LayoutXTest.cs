#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="LayoutX"/>.</summary>
	public class LayoutXTest : MonoBehaviour, IDemoImguiHarness
	{
		[Tooltip("Assign or let DemoGalleryBootstrap create Demo_LayoutX.")]
		public LayoutX layoutX;

		private void OnEnable()
		{
			if (layoutX == null)
				layoutX = FindFirstObjectByType<LayoutX>();
		}

		[ContextMenu("Mode Compact")]
		public void TestCompact()
		{
			if (!Ensure())
				return;
			layoutX.Mode = LayoutX.LayoutMode.Compact;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LayoutX Compact.");
		}

		[ContextMenu("Mode Grid")]
		public void TestGrid()
		{
			if (!Ensure())
				return;
			layoutX.Mode = LayoutX.LayoutMode.Grid;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LayoutX Grid.");
		}

		[ContextMenu("Toggle Main Axis")]
		public void TestToggleAxis()
		{
			if (!Ensure())
				return;
			layoutX.Axis = layoutX.Axis == LayoutX.MainAxis.Horizontal
				? LayoutX.MainAxis.Vertical
				: LayoutX.MainAxis.Horizontal;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LayoutX Axis={A}.", layoutX.Axis);
		}

		[ContextMenu("Toggle Reverse")]
		public void TestToggleReverse()
		{
			if (!Ensure())
				return;
			layoutX.ReverseArrangement = !layoutX.ReverseArrangement;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LayoutX Reverse={R}.", layoutX.ReverseArrangement);
		}

		[ContextMenu("Add Child")]
		public void TestAddChild()
		{
			if (!Ensure())
				return;

			var child = new GameObject("LayoutChild", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			child.transform.SetParent(layoutX.transform, false);
			var rt = child.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(48f, 32f);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LayoutX child added.");
		}

		private bool Ensure()
		{
			if (layoutX != null)
				return true;
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign LayoutX (or run Ensure Gallery).");
			return false;
		}

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, layoutX != null ? layoutX.name : "no LayoutX");
			if (UIWidgetsDemoImgui.Button(ref y, "LX Compact")) TestCompact();
			if (UIWidgetsDemoImgui.Button(ref y, "LX Grid")) TestGrid();
			if (UIWidgetsDemoImgui.Button(ref y, "LX Axis")) TestToggleAxis();
			if (UIWidgetsDemoImgui.Button(ref y, "LX Reverse")) TestToggleReverse();
			if (UIWidgetsDemoImgui.Button(ref y, "LX +Child")) TestAddChild();
		}
	}
}
#endif
