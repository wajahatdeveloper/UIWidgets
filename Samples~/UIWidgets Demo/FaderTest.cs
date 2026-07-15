#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="Fader"/>.</summary>
    public class FaderTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public float fadeDuration = 1f;

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestFadeToBlack), 0.5f);
        }

        [ContextMenu("Fade To Black")]
        public void TestFadeToBlack()
        {
            if (!Fader.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Fader instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Fader → black ({Duration}s)...", fadeDuration);
            Fader.Instance.FadeToBlack(fadeDuration);
        }

        [ContextMenu("Fade From Black")]
        public void TestFadeFromBlack()
        {
            if (!Fader.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Fader instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Fader ← black ({Duration}s)...", fadeDuration);
            Fader.Instance.FadeFromBlack(fadeDuration);
        }

        [ContextMenu("Fade Round Trip")]
        public void TestFadeRoundTrip()
        {
            if (!Fader.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Fader instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Fader round-trip...");
            Fader.Instance.FadeToBlack(fadeDuration);
            Invoke(nameof(TestFadeFromBlack), fadeDuration + 0.15f);
        }

        [ContextMenu("Cancel Fade")]
        public void TestCancelFade()
        {
            if (!Fader.HasInstance) return;
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Fader cancel.");
            Fader.Instance.CancelCurrentFade();
        }

        private void OnGUI()
        {
            float y = UIWidgetsDemoImgui.FaderY;
            if (UIWidgetsDemoImgui.Button(ref y, "Fade In")) TestFadeToBlack();
            if (UIWidgetsDemoImgui.Button(ref y, "Fade Out")) TestFadeFromBlack();
            if (UIWidgetsDemoImgui.Button(ref y, "RoundTrip")) TestFadeRoundTrip();
            if (UIWidgetsDemoImgui.Button(ref y, "Cancel")) TestCancelFade();
        }
    }
}
#endif
