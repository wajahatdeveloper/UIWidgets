#if UNITY_2019_1_OR_NEWER && !ENABLE_LEGACY_INPUT_MANAGER
#define UIWIDGETS_INPUT_SYSTEM
#endif

using UnityEngine;
#if UIWIDGETS_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace UIWidgets
{
    /// <summary>
    /// Polling input facade for UIWidgets runtime components.
    /// Compiles against whichever input backend the project has active:
    /// the Input System package when it is the sole handler, otherwise the
    /// legacy input manager. Callers keep the familiar KeyCode/button-index
    /// vocabulary either way and never touch a backend API directly.
    /// </summary>
    public static class UIInput
    {
#if UIWIDGETS_INPUT_SYSTEM
        public static Vector3 MousePosition
        {
            get
            {
                Mouse mouse = Mouse.current;
                return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
            }
        }

        public static Vector2 MouseScrollDelta
        {
            get
            {
                Mouse mouse = Mouse.current;
                return mouse != null ? mouse.scroll.ReadValue() : Vector2.zero;
            }
        }

        public static bool GetMouseButton(int button)
        {
            ButtonControl control = ResolveMouseButton(button);
            return control != null && control.isPressed;
        }

        public static bool GetMouseButtonDown(int button)
        {
            ButtonControl control = ResolveMouseButton(button);
            return control != null && control.wasPressedThisFrame;
        }

        public static bool GetMouseButtonUp(int button)
        {
            ButtonControl control = ResolveMouseButton(button);
            return control != null && control.wasReleasedThisFrame;
        }

        public static bool GetKey(KeyCode key)
        {
            KeyControl control = ResolveKey(key);
            return control != null && control.isPressed;
        }

        public static bool GetKeyDown(KeyCode key)
        {
            KeyControl control = ResolveKey(key);
            return control != null && control.wasPressedThisFrame;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            KeyControl control = ResolveKey(key);
            return control != null && control.wasReleasedThisFrame;
        }

        public static float GetAxisRaw(string axis)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return 0f;
            }

            switch (axis)
            {
                case "Horizontal":
                    return gamepad.leftStick.x.ReadValue();
                case "Vertical":
                    return gamepad.leftStick.y.ReadValue();
                default:
                    return 0f;
            }
        }

        private static ButtonControl ResolveMouseButton(int button)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return null;
            }

            switch (button)
            {
                case 0: return mouse.leftButton;
                case 1: return mouse.rightButton;
                case 2: return mouse.middleButton;
                default: return null;
            }
        }

        private static KeyControl ResolveKey(KeyCode key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return null;
            }

            // Both enums keep these ranges contiguous, so they map arithmetically.
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return keyboard[(Key)((int)Key.A + (key - KeyCode.A))];
            }
            if (key >= KeyCode.Alpha1 && key <= KeyCode.Alpha9)
            {
                return keyboard[(Key)((int)Key.Digit1 + (key - KeyCode.Alpha1))];
            }
            if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
            {
                return keyboard[(Key)((int)Key.Numpad0 + (key - KeyCode.Keypad0))];
            }
            if (key >= KeyCode.F1 && key <= KeyCode.F12)
            {
                return keyboard[(Key)((int)Key.F1 + (key - KeyCode.F1))];
            }

            switch (key)
            {
                case KeyCode.Alpha0: return keyboard.digit0Key;
                case KeyCode.Return: return keyboard.enterKey;
                case KeyCode.KeypadEnter: return keyboard.numpadEnterKey;
                case KeyCode.Space: return keyboard.spaceKey;
                case KeyCode.Escape: return keyboard.escapeKey;
                case KeyCode.Tab: return keyboard.tabKey;
                case KeyCode.Backspace: return keyboard.backspaceKey;
                case KeyCode.Delete: return keyboard.deleteKey;
                case KeyCode.UpArrow: return keyboard.upArrowKey;
                case KeyCode.DownArrow: return keyboard.downArrowKey;
                case KeyCode.LeftArrow: return keyboard.leftArrowKey;
                case KeyCode.RightArrow: return keyboard.rightArrowKey;
                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.LeftAlt: return keyboard.leftAltKey;
                case KeyCode.RightAlt: return keyboard.rightAltKey;
                case KeyCode.Home: return keyboard.homeKey;
                case KeyCode.End: return keyboard.endKey;
                case KeyCode.PageUp: return keyboard.pageUpKey;
                case KeyCode.PageDown: return keyboard.pageDownKey;
                default: return null;
            }
        }
#else
        public static Vector3 MousePosition => Input.mousePosition;

        public static Vector2 MouseScrollDelta => Input.mouseScrollDelta;

        public static bool GetMouseButton(int button) => Input.GetMouseButton(button);

        public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);

        public static bool GetMouseButtonUp(int button) => Input.GetMouseButtonUp(button);

        public static bool GetKey(KeyCode key) => Input.GetKey(key);

        public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);

        public static bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key);

        public static float GetAxisRaw(string axis) => Input.GetAxisRaw(axis);
#endif
    }
}
