#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Play Mode harness for <see cref="ModalService"/>.
    /// Assign a scene <see cref="PanelBase"/> (e.g. Containers/Panel). Dialog/InputDialog are
    /// separate singletons and are not driven through ModalService.
    /// </summary>
    public class ModalServiceTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Any PanelBase in the scene (Create via GameObject → UIWidgets → Containers → Panel).")]
        public PanelBase demoPanel;

        public bool testOnStart;

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestShow), 0.5f);
        }

        [ContextMenu("Show Modal")]
        public void TestShow()
        {
            if (!ModalService.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] ModalService instance missing in scene.");
                return;
            }

            if (demoPanel == null)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign a PanelBase to ModalServiceTest.demoPanel.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ModalService.Show({Panel})...", demoPanel.name);
            ModalService.Instance.Show(demoPanel);
        }

        [ContextMenu("Hide Current")]
        public void TestHideCurrent()
        {
            if (!ModalService.HasInstance)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] ModalService instance missing in scene.");
                return;
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ModalService.HideCurrent (open={Open}).", ModalService.Instance.IsAnyOpen);
            ModalService.Instance.HideCurrent();
        }

        [ContextMenu("Show Via PanelBase.Show")]
        public void TestShowViaPanel()
        {
            if (demoPanel == null)
            {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign a PanelBase to ModalServiceTest.demoPanel.");
                return;
            }

            if (!demoPanel.IsModal)
            {
                DebugX.Logger(LogChannels.UI).Warning(
                    "[UI:WARN:Test] Panel '{Panel}' Is Modal is off — PanelBase.Show will not route through ModalService. Enable Is Modal, or use ModalService.Show.",
                    demoPanel.name);
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] PanelBase.Show on {Panel}...", demoPanel.name);
            demoPanel.Show();
        }

        private void OnGUI()
        {
            float y = UIWidgetsDemoImgui.ModalY;
            if (UIWidgetsDemoImgui.Button(ref y, "Modal show")) TestShow();
            if (UIWidgetsDemoImgui.Button(ref y, "Modal hide")) TestHideCurrent();
            if (UIWidgetsDemoImgui.Button(ref y, "Panel.Show")) TestShowViaPanel();
            UIWidgetsDemoImgui.Label(ref y, demoPanel != null ? demoPanel.name : "assign panel");
        }
    }
}
#endif
