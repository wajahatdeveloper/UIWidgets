#if UNITY_EDITOR || DEVELOPMENT_BUILD
using FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UIWidgets
{
    /// <summary>
    /// Simple test script to verify InputDialog works with both input systems
    /// </summary>
    public class InputDialogTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool testOnStart = false;
        public string testMessage = "Enter your name:";
        public string testTitle = "Input Test";

        private void Start()
        {
            if (testOnStart)
            {
                // Small delay to ensure everything is initialized
                Invoke(nameof(TestInputDialog), 0.5f);
            }
        }

        [ContextMenu("Test Input Dialog")]
        public void TestInputDialog()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Testing InputDialog with current input system...");
            
            InputDialog.Create(testMessage)
                .WithTitle(testTitle)
                .WithPlaceholder("Type here...")
                .OnSubmit(result => 
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Input received: '{Result}'", result);
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] InputDialog test completed.");
                })
                .OnCancel(() => 
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] InputDialog test cancelled.");
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
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Validation test completed.");
                })
                .OnCancel(() => 
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Validation test cancelled.");
                })
                .Show();
        }

        [ContextMenu("Test Keyboard Shortcuts")]
        public void TestKeyboardShortcuts()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Testing InputDialog shortcuts...");
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Instructions:");
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] - ENTER submit");
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] - ESCAPE cancel");
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] - Type text and test shortcuts");
            
            InputDialog.Create("Test keyboard shortcuts:\n\n- Press ENTER to submit\n- Press ESCAPE to cancel")
                .WithTitle("Keyboard Test")
                .WithPlaceholder("Type something and test shortcuts...")
                .OnSubmit(result => 
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Submitted via keyboard: '{Result}'", result);
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Shortcut test completed.");
                })
                .OnCancel(() => 
                {
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Cancelled via keyboard.");
                    DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Shortcut test completed.");
                })
                .Show();
        }

        private void Update()
        {
            // Test keyboard shortcuts manually if needed
            // Support both legacy Input and new Input System
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tKey.wasPressedThisFrame)
                {
                    TestInputDialog();
                }
                else if (Keyboard.current.vKey.wasPressedThisFrame)
                {
                    TestInputDialogWithValidation();
                }
                else if (Keyboard.current.kKey.wasPressedThisFrame)
                {
                    TestKeyboardShortcuts();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestInputDialog();
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                TestInputDialogWithValidation();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                TestKeyboardShortcuts();
            }
#endif
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 200, 30), "Test Basic Input"))
            {
                TestInputDialog();
            }
            
            if (GUI.Button(new Rect(10, 50, 200, 30), "Test Validation"))
            {
                TestInputDialogWithValidation();
            }
            
            if (GUI.Button(new Rect(10, 90, 200, 30), "Test Keyboard Shortcuts"))
            {
                TestKeyboardShortcuts();
            }
            
            GUI.Label(new Rect(10, 130, 300, 60), "Press T, V, or K keys for quick tests\nOr use the Context Menu on this component");
        }
    }
}
#endif
