#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="LineMessage"/>.</summary>
    public class LineMessageTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public string title = "Notice";
        public string message = "Line message demo text.";
        public float duration = 4f;

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestWithTitle), 0.5f);
        }

        [ContextMenu("Show With Title")]
        public void TestWithTitle()
        {
            if (!LineMessage.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] LineMessage instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LineMessage with title...");
            LineMessage.Instance.Show(message, title, duration);
        }

        [ContextMenu("Show Message Only")]
        public void TestMessageOnly()
        {
            if (!LineMessage.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] LineMessage instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LineMessage message-only...");
            LineMessage.Instance.Show(message, "", duration);
        }

        [ContextMenu("Show Burst")]
        public void TestBurst()
        {
            if (!LineMessage.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] LineMessage instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LineMessage burst...");
            LineMessage.Instance.Show("First line", "1", duration);
            LineMessage.Instance.Show("Second line", "2", duration);
            LineMessage.Instance.Show("Third line", "3", duration);
        }

        private void OnGUI()
        {
            float y = UIWidgetsDemoImgui.LineY;
            if (UIWidgetsDemoImgui.Button(ref y, "Line+Title")) TestWithTitle();
            if (UIWidgetsDemoImgui.Button(ref y, "Line only")) TestMessageOnly();
            if (UIWidgetsDemoImgui.Button(ref y, "Line burst")) TestBurst();
        }
    }
}
#endif
