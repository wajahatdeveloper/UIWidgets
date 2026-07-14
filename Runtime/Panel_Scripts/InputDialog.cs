using System;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AetherNexus.UIWidgets
{
    [System.Serializable]
    public class InputValidation
    {
        [Header("Validation Rules")]
        public bool required = true;
        public int minLength = 0;
        public int maxLength = 1000;
        public string allowedCharacters = "";
        public string forbiddenCharacters = "";
        public bool allowEmpty = false;

        [Header("Error Messages")]
        public string requiredMessage = "This field is required";
        public string minLengthMessage = "Text is too short";
        public string maxLengthMessage = "Text is too long";
        public string invalidCharactersMessage = "Contains invalid characters";

        public bool IsValid(string input, out string errorMessage)
        {
            errorMessage = "";

            if (required && string.IsNullOrWhiteSpace(input))
            {
                errorMessage = requiredMessage;
                return false;
            }

            if (string.IsNullOrWhiteSpace(input) && !allowEmpty)
            {
                errorMessage = requiredMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(input))
            {
                if (input.Length < minLength)
                {
                    errorMessage = minLengthMessage;
                    return false;
                }

                if (input.Length > maxLength)
                {
                    errorMessage = maxLengthMessage;
                    return false;
                }

                if (!string.IsNullOrEmpty(allowedCharacters))
                {
                    foreach (char c in input)
                    {
                        if (!allowedCharacters.Contains(c.ToString()))
                        {
                            errorMessage = invalidCharactersMessage;
                            return false;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(forbiddenCharacters))
                {
                    foreach (char c in input)
                    {
                        if (forbiddenCharacters.Contains(c.ToString()))
                        {
                            errorMessage = invalidCharactersMessage;
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    public class InputDialog : SpecialDialog
    {
        // Shadow the base Instance so it resolves this concrete type's slot rather
        // than the shared Dialog slot (base uses typeof(T) == typeof(Dialog)).
        public static new InputDialog Instance => GetInstance(typeof(InputDialog)) as InputDialog;

        private static readonly Action ButtonVisibilityStub = () => { };

        [Header("Input Components")]
        public TMP_InputField inputField;
        public TextMeshProUGUI placeholderText;
        public TextMeshProUGUI errorText;
        public GameObject inputContainer;

        [Header("Input Configuration")]
        public InputValidation validation = new InputValidation();
        public string placeholder = "Enter text...";
        public bool clearOnShow = true;
        public bool selectOnShow = true;
        public bool focusOnShow = true;

        [Header("Input Events")]
        public Action<string> OnInputSubmit;
        public Action OnInputCancel;
        public Action<string> OnInputChanged;

        [Header("Debug")]
        public string lastEnteredText = "";

        private bool _isValidating;
        private string _currentError = "";
        private string _initialTextForShow = "";

        protected override void Awake()
        {
            base.Awake();
            enableKeyboardShortcuts = false;
            RewireDialogButtons();
            SetupInputField();
        }

        private void RewireDialogButtons()
        {
            if (okButton != null)
            {
                okButton.OnClicked.RemoveListener(OnClick_Ok);
                okButton.OnClicked.AddListener(HandleOkClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.OnClicked.RemoveListener(OnClick_Cancel);
                cancelButton.OnClicked.AddListener(HandleCancelClicked);
            }

            if (closeButton != null)
            {
                closeButton.OnClicked.RemoveListener(OnClick_Close);
                closeButton.OnClicked.AddListener(HandleCloseClicked);
            }
        }

        private void SetupInputField()
        {
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(OnInputValueChanged);
                inputField.onEndEdit.AddListener(OnInputEndEdit);
            }
        }

        public static new InputDialogBuilder Create(string message)
        {
            return new InputDialogBuilder(message);
        }

        public virtual void ShowInput(
            string message,
            string title,
            Action<string> onSubmit,
            Action onCancel,
            Dialog.DialogIcon iconType,
            Sprite customIcon,
            string initialText)
        {
            OnInputSubmit = onSubmit;
            OnInputCancel = onCancel;
            _initialTextForShow = initialText;

            Action onCancelVisibility = onCancel != null ? ButtonVisibilityStub : null;
            Show(message, title, null, null, ButtonVisibilityStub, onCancelVisibility, onCancelVisibility, iconType, customIcon);
        }

        public virtual void ShowInputWithValidation(
            string message,
            string title,
            InputValidation validationRules,
            Action<string> onValidSubmit,
            Action onCancel,
            Dialog.DialogIcon iconType,
            Sprite customIcon,
            string initialText)
        {
            if (validationRules == null)
                validation = new InputValidation();
            else
                validation = validationRules;

            ShowInput(message, title, onValidSubmit, onCancel, iconType, customIcon, initialText);
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            if (inputField != null)
            {
                if (!string.IsNullOrEmpty(_initialTextForShow))
                    inputField.text = _initialTextForShow;
                else if (clearOnShow)
                    inputField.text = "";

                if (placeholderText != null)
                    placeholderText.text = placeholder;
            }

            _initialTextForShow = "";
            ClearError();

            if (inputContainer != null)
                inputContainer.SetActive(true);
        }

        protected override void OnAfterShow()
        {
            base.OnAfterShow();

            if (inputField != null && focusOnShow)
            {
                inputField.ActivateInputField();
                if (selectOnShow)
                    inputField.Select();
            }
        }

        protected override void OnBeforeHide()
        {
            base.OnBeforeHide();

            if (inputField != null)
                lastEnteredText = inputField.text;
        }

        protected override void OnAfterHide()
        {
            base.OnAfterHide();

            if (inputField != null)
                inputField.text = "";

            ClearError();
            OnInputSubmit = null;
            OnInputCancel = null;
            OnInputChanged = null;
        }

        private void OnInputValueChanged(string value)
        {
            OnInputChanged?.Invoke(value);

            if (!string.IsNullOrEmpty(_currentError))
                ClearError();
        }

        private void OnInputEndEdit(string value)
        {
            if (IsEnterKeyPressed())
                TrySubmit();
        }

        private void HandleOkClicked()
        {
            TrySubmit();
        }

        private void HandleCancelClicked()
        {
            OnInputCancel?.Invoke();
            Hide();
        }

        private void HandleCloseClicked()
        {
            if (OnInputCancel != null)
                OnInputCancel.Invoke();
            Hide();
        }

        private void TrySubmit()
        {
            if (_isValidating)
                return;

            _isValidating = true;
            try
            {
                string input = inputField != null ? inputField.text : "";

                if (validation.IsValid(input, out string errorMessage))
                {
                    OnInputSubmit?.Invoke(input);
                    Hide();
                }
                else
                {
                    ShowError(errorMessage);
                }
            }
            finally
            {
                _isValidating = false;
            }
        }

        private void ShowError(string message)
        {
            _currentError = message;
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            _currentError = "";
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (panel == null || !panel.activeInHierarchy)
                return;

            if (IsEscapeKeyPressed())
            {
                if (OnInputCancel != null && cancelButton != null && cancelButton.gameObject.activeInHierarchy)
                    HandleCancelClicked();
                else if (enableCloseButton && closeButton != null && closeButton.gameObject.activeInHierarchy)
                    HandleCloseClicked();
                return;
            }

            if (IsEnterKeyPressed())
                TrySubmit();
        }

        private bool IsEnterKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return ReadEnterPressedFromNewInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#else
            return ReadEnterPressedFromNewInputSystem();
#endif
        }

        private bool IsEscapeKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return ReadEscapePressedFromNewInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return ReadEscapePressedFromNewInputSystem();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool ReadEnterPressedFromNewInputSystem()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                    return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                return true;

            return false;
        }

        private static bool ReadEscapePressedFromNewInputSystem()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                return true;

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
                return false;

            return gamepad.buttonEast.wasPressedThisFrame
                || gamepad.startButton.wasPressedThisFrame
                || gamepad.selectButton.wasPressedThisFrame;
        }
#else
        private static bool ReadEnterPressedFromNewInputSystem() => false;

        private static bool ReadEscapePressedFromNewInputSystem() => false;
#endif

        public void SetValidation(InputValidation newValidation)
        {
            if (newValidation == null)
                validation = new InputValidation();
            else
                validation = newValidation;
        }

        public void SetPlaceholder(string placeholderText)
        {
            placeholder = placeholderText;
            if (this.placeholderText != null)
                this.placeholderText.text = placeholderText;
        }

        public void SetInputText(string text)
        {
            if (inputField != null)
                inputField.text = text;
        }

        public string GetInputText()
        {
            return inputField != null ? inputField.text : "";
        }
    }

    public class InputDialogBuilder
    {
        private readonly string _message;
        private string _title = "";
        private Action<string> _onSubmit;
        private Action _onCancel;
        private InputValidation _validation = new InputValidation();
        private string _placeholder = "Enter text...";
        private Dialog.DialogIcon _iconType = Dialog.DialogIcon.None;
        private Sprite _customIcon;
        private string _initialText = "";

        internal InputDialogBuilder(string message)
        {
            _message = message;
        }

        public InputDialogBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public InputDialogBuilder OnSubmit(Action<string> callback)
        {
            _onSubmit = callback;
            return this;
        }

        public InputDialogBuilder OnCancel(Action callback)
        {
            _onCancel = callback;
            return this;
        }

        public InputDialogBuilder WithValidation(InputValidation validationRules)
        {
            if (validationRules == null)
                _validation = new InputValidation();
            else
                _validation = validationRules;
            return this;
        }

        public InputDialogBuilder WithPlaceholder(string placeholder)
        {
            _placeholder = placeholder;
            return this;
        }

        public InputDialogBuilder WithInitialText(string initialText)
        {
            _initialText = initialText;
            return this;
        }

        public InputDialogBuilder WithIcon(Dialog.DialogIcon iconType)
        {
            _iconType = iconType;
            return this;
        }

        public InputDialogBuilder WithCustomIcon(Sprite customSprite)
        {
            _customIcon = customSprite;
            return this;
        }

        public void Show()
        {
            var inputDialog = InputDialog.Instance as InputDialog;
            if (inputDialog == null)
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Panel] InputDialog instance not found.");
                return;
            }

            inputDialog.SetPlaceholder(_placeholder);
            inputDialog.SetValidation(_validation);
            inputDialog.ShowInput(_message, _title, _onSubmit, _onCancel, _iconType, _customIcon, _initialText);
        }
    }
}
