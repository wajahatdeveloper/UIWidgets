using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIWidgets
{
    public class DefaultEnter : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Button defaultButton;

        [Header("Activation Keys")]
        [Tooltip("Keys that trigger the default button.")]
        [SerializeField] private KeyCode[] triggerKeys = new[] { KeyCode.Return, KeyCode.KeypadEnter };

        [Header("Behavior")]
        [Tooltip("Delay before listening to input.")]
        [SerializeField] private float startDelay = 0.0f;
        [Tooltip("Cooldown after a trigger before another can occur.")]
        [SerializeField] private float resetDelay = 0.25f;
        [Tooltip("Hold a key for duration to trigger instead of on key down.")]
        [SerializeField] private bool useHoldToActivate = false;
        [Tooltip("Seconds the key must be held if hold-to-activate is enabled.")]
        [SerializeField] private float holdDuration = 0.4f;
        [Tooltip("Require the button to be interactable and enabled.")]
        [SerializeField] private bool requireInteractable = true;
        [Tooltip("Only allow trigger if this GameObject is active and enabled.")]
        [SerializeField] private bool requireActive = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onTriggered;

        private bool _shouldUpdate = false;
        private bool _coolingDown = false;
        private float _holdTimer = 0f;

        private IEnumerator Start()
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }
            _shouldUpdate = true;
            _coolingDown = false;
            _holdTimer = 0f;
        }

        private void OnDisable()
        {
            _holdTimer = 0f;
            _coolingDown = false;
        }

        private void Update()
        {
            if (!_shouldUpdate) { return; }
            if (_coolingDown) { return; }
            if (requireActive && (!isActiveAndEnabled || !gameObject.activeInHierarchy)) { return; }

            if (defaultButton == null)
            {
                return;
            }

            if (requireInteractable && (!defaultButton.enabled || !defaultButton.interactable))
            {
                return;
            }

            bool anyKeyDown = false;
            bool anyKeyHeld = false;
            for (int i = 0; i < triggerKeys.Length; i++)
            {
                KeyCode code = triggerKeys[i];
                // Route through the input facade: it reads the active backend (New Input System
                // here) and never touches the legacy UnityEngine.Input API, which throws under
                // activeInputHandler == InputSystem.
                if (UIInput.GetKeyDown(code)) { anyKeyDown = true; }
                if (UIInput.GetKey(code)) { anyKeyHeld = true; }
            }

            if (!useHoldToActivate)
            {
                if (anyKeyDown)
                {
                    Trigger();
                }
                return;
            }

            // Hold-to-activate
            if (anyKeyHeld)
            {
                _holdTimer += Time.unscaledDeltaTime;
                if (_holdTimer >= Mathf.Max(0f, holdDuration))
                {
                    Trigger();
                }
            }
            else
            {
                _holdTimer = 0f;
            }
        }

        private void Trigger()
        {
            _coolingDown = true;
            onTriggered?.Invoke();
            defaultButton.onClick?.Invoke();
            StartCoroutine(ResetClick());
        }

        private IEnumerator ResetClick()
        {
            _holdTimer = 0f;
            if (resetDelay > 0f)
            {
                yield return new WaitForSeconds(resetDelay);
            }
            _coolingDown = false;
        }
    }
}
