#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="PopupText"/>.</summary>
	public class PopupTextTest : MonoBehaviour
	{
		[Tooltip("World / screen anchor for spawn. Defaults to this transform.")]
		public Transform spawnAnchor;

		private void OnEnable()
		{
			if (spawnAnchor == null)
				spawnAnchor = transform;
		}

		[ContextMenu("Show Popup")]
		public void TestShow()
		{
			if (!PopupText.HasInstance)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] PopupText missing — spawn Singletons/PopupText.");
				return;
			}

			var pos = spawnAnchor != null ? spawnAnchor.position : Vector3.zero;
			PopupText.Instance.Show(pos, "+25", Color.yellow);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] PopupText shown.");
		}

		private void OnGUI()
		{
			if (!UIWidgetsDemoImgui.IsSection(UIWidgetsDemoImgui.Section.Feedback))
				return;

			float y = UIWidgetsDemoImgui.ContentY + 160f;
			UIWidgetsDemoImgui.Label(ref y, PopupText.HasInstance ? "Popup ok" : "need Popup");
			if (UIWidgetsDemoImgui.Button(ref y, "Popup +25")) TestShow();
		}
	}
}
#endif
