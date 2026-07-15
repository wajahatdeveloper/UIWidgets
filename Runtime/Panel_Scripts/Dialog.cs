using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    public class Dialog : SingletonBehaviour<Dialog>
    {
        public enum Layout
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel
        }

        public enum DialogIcon
        {
            None,
            Info,
            Warning,
            Error,
            Custom
        }

        public GameObject panel;
    
        public TextMeshProUGUI title;
        public TextMeshProUGUI message;
        public Image icon;

        public ButtonX yesButton;
        public ButtonX noButton;
        public ButtonX okButton;
        public ButtonX cancelButton;
        public ButtonX closeButton;

        public Action OnYes;
        public Action OnNo;
        public Action OnOk;
        public Action OnCancel;
        public Action OnClose;

        [Header("Behavior")]
        public bool enableLogging = true;
        public bool enableKeyboardShortcuts = true;
        public bool enableCloseButton = true;

        [Header("Icons")]
        [InspectorName("Built-in Icons (Assets)")]
        public List<Sprite> builtInIcons = new List<Sprite>();
        public DialogIcon currentIcon = DialogIcon.None;
        public Sprite customIcon;

        protected override void Awake()
        {
            base.Awake();
            // Wire up button listeners once
            if (yesButton != null)
            {
                yesButton.OnClicked.RemoveListener(OnClick_Yes);
                yesButton.OnClicked.AddListener(OnClick_Yes);
            }
            if (noButton != null)
            {
                noButton.OnClicked.RemoveListener(OnClick_No);
                noButton.OnClicked.AddListener(OnClick_No);
            }
            if (okButton != null)
            {
                okButton.OnClicked.RemoveListener(OnClick_Ok);
                okButton.OnClicked.AddListener(OnClick_Ok);
            }
            if (cancelButton != null)
            {
                cancelButton.OnClicked.RemoveListener(OnClick_Cancel);
                cancelButton.OnClicked.AddListener(OnClick_Cancel);
            }
            if (closeButton != null)
            {
                closeButton.OnClicked.RemoveListener(OnClick_Close);
                closeButton.OnClicked.AddListener(OnClick_Close);
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        // Fluent API Builder
        public static DialogBuilder Create(string message)
        {
            return new DialogBuilder(message);
        }

        // Fluent API Builder Class
        public class DialogBuilder
        {
            private string _message;
            private string _title = "";
            private Action _onYes = null;
            private Action _onNo = null;
            private Action _onOk = null;
            private Action _onCancel = null;
            private Action _onClose = null;
            private Layout _layout = Layout.Ok;
            private DialogIcon _iconType = DialogIcon.None;
            private Sprite _customIcon = null;

            internal DialogBuilder(string message)
            {
                _message = message;
            }

            public DialogBuilder WithTitle(string title)
            {
                _title = title;
                return this;
            }

            public DialogBuilder OnYes(Action callback)
            {
                _onYes = callback;
                return this;
            }

            public DialogBuilder OnNo(Action callback)
            {
                _onNo = callback;
                return this;
            }

            public DialogBuilder OnOk(Action callback)
            {
                _onOk = callback;
                return this;
            }

            public DialogBuilder OnCancel(Action callback)
            {
                _onCancel = callback;
                return this;
            }

            public DialogBuilder OnClose(Action callback)
            {
                _onClose = callback;
                return this;
            }

            public DialogBuilder WithLayout(Layout layout)
            {
                _layout = layout;
                return this;
            }

            public DialogBuilder WithIcon(DialogIcon iconType)
            {
                _iconType = iconType;
                _customIcon = null; // Clear custom icon when using built-in
                return this;
            }

            public DialogBuilder WithCustomIcon(Sprite customSprite)
            {
                _customIcon = customSprite;
                _iconType = DialogIcon.Custom;
                return this;
            }

            public void Show()
            {
                Instance.Show(_message, _title, _onYes, _onNo, _onOk, _onCancel, _onClose, _iconType, _customIcon);
            }

            public void ShowWithLayout()
            {
                // Map fluent callbacks onto layout roles (primary / secondary / tertiary).
                Action primary = null;
                Action secondary = null;
                Action tertiary = null;
                switch (_layout)
                {
                    case Layout.Ok:
                        primary = _onOk ?? _onYes;
                        break;
                    case Layout.OkCancel:
                        primary = _onOk ?? _onYes;
                        secondary = _onCancel ?? _onNo;
                        break;
                    case Layout.YesNo:
                        primary = _onYes;
                        secondary = _onNo;
                        break;
                    case Layout.YesNoCancel:
                        primary = _onYes;
                        secondary = _onNo;
                        tertiary = _onCancel;
                        break;
                }

                Instance.Show(_message, _title, _layout, primary, secondary, tertiary, _onClose, _iconType, _customIcon);
            }
        }

        public virtual void Show(string text, string titleText="", Action onYes=null, Action onNo=null, Action onOk=null, Action onCancel=null, Action onClose=null, DialogIcon iconType=DialogIcon.None, Sprite customIconSprite=null)
        {
            if (message != null) message.text = text;
            if (title != null) title.text = titleText;

            SetActionAndButton(onYes, out OnYes, yesButton);
            SetActionAndButton(onNo, out OnNo, noButton);
            SetActionAndButton(onCancel, out OnCancel, cancelButton);
            SetActionAndButton(onOk, out OnOk, okButton);
            SetActionAndButton(onClose, out OnClose, closeButton);

            // Set close button availability
            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(enableCloseButton);
            }

            // Set icon
            if (customIconSprite != null)
            {
                SetCustomIcon(customIconSprite);
            }
            else
            {
                SetIcon(iconType);
            }

            if (panel != null)
            {
                panel.SetActive(true);
                // Try set initial focus to the first active & interactable button
                TryFocusDefaultButton();
            }
            else
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Panel] Dialog.panel is not assigned; cannot show dialog.");
            }

            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog Shown in {SceneName}", SceneManager.GetActiveScene().name);
        }

        public virtual void Show(string text, string titleText, Layout layout, Action onPrimary=null, Action onSecondary=null, Action onTertiary=null, Action onClose=null, DialogIcon iconType=DialogIcon.None, Sprite customIconSprite=null)
        {
            // Layout always shows its buttons (callbacks may be null — click still dismisses).
            // Ok:            primary -> OK
            // OkCancel:      primary -> OK, secondary -> Cancel
            // YesNo:         primary -> Yes, secondary -> No
            // YesNoCancel:   primary -> Yes, secondary -> No, tertiary -> Cancel

            if (message != null) message.text = text;
            if (title != null) title.text = titleText;

            BindButton(yesButton, out OnYes, null, false);
            BindButton(noButton, out OnNo, null, false);
            BindButton(cancelButton, out OnCancel, null, false);
            BindButton(okButton, out OnOk, null, false);
            OnClose = onClose;

            switch (layout)
            {
                case Layout.Ok:
                    BindButton(okButton, out OnOk, onPrimary, true);
                    break;
                case Layout.OkCancel:
                    BindButton(okButton, out OnOk, onPrimary, true);
                    BindButton(cancelButton, out OnCancel, onSecondary, true);
                    break;
                case Layout.YesNo:
                    BindButton(yesButton, out OnYes, onPrimary, true);
                    BindButton(noButton, out OnNo, onSecondary, true);
                    break;
                case Layout.YesNoCancel:
                    BindButton(yesButton, out OnYes, onPrimary, true);
                    BindButton(noButton, out OnNo, onSecondary, true);
                    BindButton(cancelButton, out OnCancel, onTertiary, true);
                    break;
            }

            if (closeButton != null)
                closeButton.gameObject.SetActive(enableCloseButton);

            if (customIconSprite != null)
                SetCustomIcon(customIconSprite);
            else
                SetIcon(iconType);

            if (panel != null)
            {
                panel.SetActive(true);
                TryFocusDefaultButton();
            }
            else
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Panel] Dialog.panel is not assigned; cannot show dialog.");
            }

            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog Shown ({Layout}) in {SceneName}", layout, SceneManager.GetActiveScene().name);
        }

        public virtual void Hide()
        {
            OnYes = null;
            OnNo = null;
            OnCancel = null;
            OnOk = null;
            OnClose = null;
        
            if (panel != null) panel.SetActive(false);
        }

        private void SetActionAndButton(Action actionToAssign, out Action myAction, ButtonX button)
        {
            BindButton(button, out myAction, actionToAssign, actionToAssign != null);
        }

        private static void BindButton(ButtonX button, out Action myAction, Action action, bool visible)
        {
            myAction = action;
            if (button != null)
                button.gameObject.SetActive(visible);
        }

        public void OnClick_Yes()
        {
            OnYes?.Invoke();

            Hide();
            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog (OnYes) Hidden in {SceneName}", SceneManager.GetActiveScene().name);
        }

        public void OnClick_No()
        {
            OnNo?.Invoke();

            Hide();
            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog (OnNo) Hidden in {SceneName}", SceneManager.GetActiveScene().name);
        }

        public void OnClick_Cancel()
        {
            OnCancel?.Invoke();

            Hide();
            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog (OnCancel) Hidden in {SceneName}", SceneManager.GetActiveScene().name);
        }

        public void OnClick_Ok()
        {
            OnOk?.Invoke();
        
            Hide();
            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog (OnOK) Hidden in {SceneName}", SceneManager.GetActiveScene().name);
        }

        public void OnClick_Close()
        {
            OnClose?.Invoke();

            Hide();
            if (enableLogging)
                DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] Dialog (OnClose) Hidden in {SceneName}", SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Sets the dialog icon using the built-in icon enum
        /// </summary>
        public void SetIcon(DialogIcon iconType)
        {
            currentIcon = iconType;
            UpdateIconDisplay();
        }

        /// <summary>
        /// Sets a custom icon sprite
        /// </summary>
        public void SetCustomIcon(Sprite customSprite)
        {
            customIcon = customSprite;
            currentIcon = DialogIcon.Custom;
            UpdateIconDisplay();
        }

        /// <summary>
        /// Clears the current icon
        /// </summary>
        public void ClearIcon()
        {
            currentIcon = DialogIcon.None;
            customIcon = null;
            UpdateIconDisplay();
        }

        /// <summary>
        /// Updates the icon display based on current settings
        /// </summary>
        private void UpdateIconDisplay()
        {
            if (icon == null) return;

            switch (currentIcon)
            {
                case DialogIcon.None:
                    icon.gameObject.SetActive(false);
                    break;
                case DialogIcon.Info:
                case DialogIcon.Warning:
                case DialogIcon.Error:
                    if (builtInIcons != null && builtInIcons.Count > (int)currentIcon - 1)
                    {
                        icon.sprite = builtInIcons[(int)currentIcon - 1];
                        icon.gameObject.SetActive(true);
                    }
                    else
                    {
                        icon.gameObject.SetActive(false);
                    }
                    break;
                case DialogIcon.Custom:
                    if (customIcon != null)
                    {
                        icon.sprite = customIcon;
                        icon.gameObject.SetActive(true);
                    }
                    else
                    {
                        icon.gameObject.SetActive(false);
                    }
                    break;
            }
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts) return;
            if (panel == null || !panel.activeInHierarchy) return;

            // New Input System only (project's activeInputHandler == InputSystem).
            bool enterPressed = false;
            bool escapePressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                enterPressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
                escapePressed = keyboard.escapeKey.wasPressedThisFrame;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                enterPressed |= gamepad.buttonSouth.wasPressedThisFrame;
                escapePressed |= gamepad.buttonEast.wasPressedThisFrame
                              || gamepad.startButton.wasPressedThisFrame
                              || gamepad.selectButton.wasPressedThisFrame;
            }

            if (enterPressed)
            {
                // Prefer OK then YES if visible
                if (okButton != null && okButton.gameObject.activeInHierarchy && okButton.Interactable)
                {
                    OnClick_Ok();
                    return;
                }
                if (yesButton != null && yesButton.gameObject.activeInHierarchy && yesButton.Interactable)
                {
                    OnClick_Yes();
                    return;
                }
            }

            if (escapePressed)
            {
                // Prefer Cancel then No if visible
                if (cancelButton != null && cancelButton.gameObject.activeInHierarchy && cancelButton.Interactable)
                {
                    OnClick_Cancel();
                    return;
                }
                if (noButton != null && noButton.gameObject.activeInHierarchy && noButton.Interactable)
                {
                    OnClick_No();
                    return;
                }
            }
        }

        private void TryFocusDefaultButton()
        {
            // Priority: OK, Yes, Cancel, No
            if (okButton != null && okButton.gameObject.activeInHierarchy && okButton.Interactable)
            {
                okButton.Select();
                return;
            }
            if (yesButton != null && yesButton.gameObject.activeInHierarchy && yesButton.Interactable)
            {
                yesButton.Select();
                return;
            }
            if (cancelButton != null && cancelButton.gameObject.activeInHierarchy && cancelButton.Interactable)
            {
                cancelButton.Select();
                return;
            }
            if (noButton != null && noButton.gameObject.activeInHierarchy && noButton.Interactable)
            {
                noButton.Select();
                return;
            }
        }
    }
}