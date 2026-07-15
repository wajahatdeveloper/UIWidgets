#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="LoadingPanel"/>.</summary>
    public class LoadingPanelTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public string loadingText = "Loading demo...";
        public float timedSeconds = 3f;

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestTimedProgress), 0.5f);
        }

        [ContextMenu("Show Simple")]
        public void TestShowSimple()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LoadingPanel simple...");
            LoadingPanel.Create(loadingText).Show();
        }

        [ContextMenu("Show Timed Progress")]
        public void TestTimedProgress()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LoadingPanel timed progress ({Seconds}s)...", timedSeconds);
            LoadingPanel.Create(loadingText)
                .WithProgressBar()
                .WithTimer(timedSeconds)
                .Show();
            Invoke(nameof(TestHide), timedSeconds + 0.25f);
        }

        [ContextMenu("Hide")]
        public void TestHide()
        {
            if (!LoadingPanel.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] LoadingPanel instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] LoadingPanel hide.");
            LoadingPanel.Instance.HideIfShown();
        }

        private void OnGUI()
        {
            float y = UIWidgetsDemoImgui.LoadingY;
            if (UIWidgetsDemoImgui.Button(ref y, "Load show")) TestShowSimple();
            if (UIWidgetsDemoImgui.Button(ref y, "Load bar")) TestTimedProgress();
            if (UIWidgetsDemoImgui.Button(ref y, "Load hide")) TestHide();
        }
    }
}
#endif
