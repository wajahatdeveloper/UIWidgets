#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="WaitPanel"/>.</summary>
    public class WaitPanelTest : MonoBehaviour, IDemoImguiHarness
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public string waitText = "Please wait...";
        public float autoHideSeconds = 2f;

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestShowThenHide), 0.5f);
        }

        [ContextMenu("Show")]
        public void TestShow()
        {
            if (!WaitPanel.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] WaitPanel instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] WaitPanel show...");
            WaitPanel.Instance.Show(waitText);
        }

        [ContextMenu("Hide")]
        public void TestHide()
        {
            if (!WaitPanel.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] WaitPanel instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] WaitPanel hide.");
            WaitPanel.Instance.Hide();
        }

        [ContextMenu("Show Then Hide")]
        public void TestShowThenHide()
        {
            TestShow();
            Invoke(nameof(TestHide), autoHideSeconds);
        }

        [ContextMenu("Show Counted (+1)")]
        public void TestShowCounted()
        {
            if (!WaitPanel.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] WaitPanel instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] WaitPanel ShowCounted...");
            WaitPanel.Instance.ShowCounted(waitText + " (counted)");
        }

        [ContextMenu("Hide Counted (-1)")]
        public void TestHideCounted()
        {
            if (!WaitPanel.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] WaitPanel instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] WaitPanel HideCounted.");
            WaitPanel.Instance.HideCounted();
        }

        public void DrawImgui(ref float y)
        {
            if (UIWidgetsDemoImgui.Button(ref y, "Wait show")) TestShow();
            if (UIWidgetsDemoImgui.Button(ref y, "Wait hide")) TestHide();
            if (UIWidgetsDemoImgui.Button(ref y, "Wait +")) TestShowCounted();
            if (UIWidgetsDemoImgui.Button(ref y, "Wait -")) TestHideCounted();
        }
    }
}
#endif
