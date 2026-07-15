#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="Toast"/> / <see cref="ToastUI"/>.</summary>
	public class ToastTest : MonoBehaviour, IDemoImguiHarness
	{
		[ContextMenu("Toast Green")]
		public void TestGreen()
		{
			Toast.Create("Saved")
				.WithDuration(2f)
				.WithSeverity(ToastSeverity.Success)
				.AtPosition(ToastPosition.TopRight)
				.Show();
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Toast green.");
		}

		[ContextMenu("Toast Red")]
		public void TestRed()
		{
			Toast.Create("Error example")
				.WithDuration(2.5f)
				.WithSeverity(ToastSeverity.Error)
				.AtPosition(ToastPosition.BottomCenter)
				.ClickToDismiss(true)
				.Show();
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Toast red (click dismiss).");
		}

		[ContextMenu("Toast Queue")]
		public void TestQueue()
		{
			Toast.Create("First").WithDuration(1.2f).WithSeverity(ToastSeverity.Info).Show();
			Toast.Create("Second").WithDuration(1.2f).WithSeverity(ToastSeverity.Warning).Show();
			Toast.Create("Third").WithDuration(1.2f).WithSeverity(ToastSeverity.Success).Show();
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Toast queue x3.");
		}

		[ContextMenu("Toast Black")]
		public void TestBlack()
		{
			Toast.Create("Black contrast check")
				.WithDuration(2f)
				.WithColor(ToastColor.Black)
				.AtPosition(ToastPosition.MiddleCenter)
				.Show();
		}

		[ContextMenu("Dismiss")]
		public void TestDismiss() => Toast.Dismiss();

		[ContextMenu("Dismiss All")]
		public void TestDismissAll() => Toast.DismissAll();

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, ToastUI.HasInstance ? "ToastUI ok" : "need ToastUI");
			UIWidgetsDemoImgui.Label(ref y,
				ToastUI.HasInstance
					? $"showing:{ToastUI.Instance.IsShowing} q:{ToastUI.Instance.QueuedCount}"
					: "showing:- q:-");
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Green")) TestGreen();
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Red")) TestRed();
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Queue")) TestQueue();
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Black")) TestBlack();
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Dismiss")) TestDismiss();
			if (UIWidgetsDemoImgui.Button(ref y, "Toast Dismiss All")) TestDismissAll();
		}
	}
}
#endif
