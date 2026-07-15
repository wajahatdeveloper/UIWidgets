#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="PopupText"/>.</summary>
	public class PopupTextTest : MonoBehaviour, IDemoImguiHarness
	{
		[Tooltip("Optional world anchor. If empty, spawns at screen center in front of the main camera.")]
		public Transform spawnAnchor;

		[ContextMenu("Show Popup")]
		public void TestShow()
		{
			if (!PopupText.HasInstance)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] PopupText missing — spawn Singletons/PopupText.");
				return;
			}

			PopupText.Instance.Show(ResolveSpawnPosition(), "+25", Color.yellow);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] PopupText shown.");
		}

		private Vector3 ResolveSpawnPosition()
		{
			if (spawnAnchor != null)
				return spawnAnchor.position;

			var cam = Camera.main;
			if (cam != null)
				return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.55f, 5f));

			return Vector3.zero;
		}

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, PopupText.HasInstance ? "Popup ok" : "need Popup");
			if (UIWidgetsDemoImgui.Button(ref y, "Popup +25")) TestShow();
		}
	}
}
#endif
