#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="Dialog"/> layouts and callbacks.</summary>
    public class DialogTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public string testMessage = "Do you want to continue?";
        public string testTitle = "Dialog Test";

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestYesNo), 0.5f);
        }

        [ContextMenu("Test Ok")]
        public void TestOk()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Ok...");
            Dialog.Create(testMessage)
                .WithTitle(testTitle)
                .WithLayout(Dialog.Layout.Ok)
                .WithIcon(Dialog.DialogIcon.Info)
                .OnOk(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Ok pressed."))
                .ShowWithLayout();
        }

        [ContextMenu("Test Yes/No")]
        public void TestYesNo()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog YesNo...");
            Dialog.Create(testMessage)
                .WithTitle(testTitle)
                .WithLayout(Dialog.Layout.YesNo)
                .WithIcon(Dialog.DialogIcon.Warning)
                .OnYes(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Yes."))
                .OnNo(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog No."))
                .ShowWithLayout();
        }

        [ContextMenu("Test Ok/Cancel")]
        public void TestOkCancel()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog OkCancel...");
            Dialog.Create("Confirm this action?")
                .WithTitle("Confirm")
                .WithLayout(Dialog.Layout.OkCancel)
                .WithIcon(Dialog.DialogIcon.Error)
                .OnOk(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Ok (OkCancel)."))
                .OnCancel(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Cancel."))
                .ShowWithLayout();
        }

        [ContextMenu("Test Yes/No/Cancel")]
        public void TestYesNoCancel()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog YesNoCancel...");
            Dialog.Create("Save changes before closing?")
                .WithTitle("Unsaved")
                .WithLayout(Dialog.Layout.YesNoCancel)
                .OnYes(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Yes (YNC)."))
                .OnNo(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog No (YNC)."))
                .OnCancel(() => DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Dialog Cancel (YNC)."))
                .ShowWithLayout();
        }

        private void OnGUI()
        {
            if (!UIWidgetsDemoImgui.IsSection(UIWidgetsDemoImgui.Section.Modals))
                return;

            float y = UIWidgetsDemoImgui.ContentY;
            if (UIWidgetsDemoImgui.Button(ref y, "Dlg Ok")) TestOk();
            if (UIWidgetsDemoImgui.Button(ref y, "Dlg YesNo")) TestYesNo();
            if (UIWidgetsDemoImgui.Button(ref y, "Dlg OkCancel")) TestOkCancel();
            if (UIWidgetsDemoImgui.Button(ref y, "Dlg Y/N/C")) TestYesNoCancel();
        }
    }
}
#endif
