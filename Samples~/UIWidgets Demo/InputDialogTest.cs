#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Play Mode harness for <see cref="InputDialog"/>.</summary>
    public class InputDialogTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart;
        public string testMessage = "Enter your name:";
        public string testTitle = "Input Test";

        private void Start()
        {
            if (testOnStart)
                Invoke(nameof(TestInputDialog), 0.5f);
        }

        [ContextMenu("Test Input Dialog")]
        public void TestInputDialog()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Testing InputDialog...");
            InputDialog.Create(testMessage)
                .WithTitle(testTitle)
                .WithPlaceholder("Type here...")
                .OnSubmit(result =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Input received: '{Result}'", result);
                })
                .OnCancel(() =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] InputDialog cancelled.");
                })
                .Show();
        }

        [ContextMenu("Test Input Dialog with Validation")]
        public void TestInputDialogWithValidation()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Testing InputDialog validation...");
            var validation = new InputValidation
            {
                required = true,
                minLength = 2,
                maxLength = 20,
                requiredMessage = "Name is required",
                minLengthMessage = "Name must be at least 2 characters",
                maxLengthMessage = "Name must be less than 20 characters"
            };

            InputDialog.Create("Enter a name (2-20 characters):")
                .WithTitle("Validation Test")
                .WithValidation(validation)
                .WithPlaceholder("Enter name...")
                .OnSubmit(result =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Valid input: '{Result}'", result);
                })
                .OnCancel(() =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Validation cancelled.");
                })
                .Show();
        }

        [ContextMenu("Test Keyboard Shortcuts")]
        public void TestKeyboardShortcuts()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] InputDialog shortcuts — ENTER submit, ESCAPE cancel.");
            InputDialog.Create("Test keyboard shortcuts:\n\n- Press ENTER to submit\n- Press ESCAPE to cancel")
                .WithTitle("Keyboard Test")
                .WithPlaceholder("Type something and test shortcuts...")
                .OnSubmit(result =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Submitted: '{Result}'", result);
                })
                .OnCancel(() =>
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Cancelled via keyboard.");
                })
                .Show();
        }

        private void OnGUI()
        {
            if (!UIWidgetsDemoImgui.IsSection(UIWidgetsDemoImgui.Section.Modals))
                return;

            float y = UIWidgetsDemoImgui.ContentY + 90f;
            if (UIWidgetsDemoImgui.Button(ref y, "Input")) TestInputDialog();
            if (UIWidgetsDemoImgui.Button(ref y, "Validate")) TestInputDialogWithValidation();
            if (UIWidgetsDemoImgui.Button(ref y, "Shortcuts")) TestKeyboardShortcuts();
        }
    }
}
#endif
