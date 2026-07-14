using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    [DisallowMultipleComponent]
    public class AutoClick : MonoBehaviour
    {
        [SerializeField]
        private float delay = 0.2f;

        [SerializeField]
        private bool invokeOnEnable = true;

        [SerializeField]
        private bool repeat = false;

        [SerializeField]
        private float repeatInterval = 1f;

        [SerializeField]
        private bool cancelPendingOnDisable = true;

        [SerializeField]
        private bool requireInteractable = true;

        [SerializeField]
        private Button targetButton;

        private Button cachedButton;

        private void Awake()
        {
            cachedButton = targetButton != null ? targetButton : GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (!invokeOnEnable)
            {
                return;
            }

            if (repeat)
            {
                InvokeRepeating(nameof(Execute), delay, Mathf.Max(0.0001f, repeatInterval));
            }
            else
            {
                Invoke(nameof(Execute), delay);
            }
        }

        private void OnDisable()
        {
            if (!cancelPendingOnDisable)
            {
                return;
            }

            CancelInvoke(nameof(Execute));
        }

        [ContextMenu("Execute Now")]
        public void Execute()
        {
            var button = cachedButton != null ? cachedButton : (cachedButton = GetComponent<Button>());
            if (button == null)
            {
                return;
            }

            if (requireInteractable && !button.interactable)
            {
                return;
            }

            button.onClick?.Invoke();
        }

        // Expose setters for runtime control if needed
        public void SetTargetButton(Button button)
        {
            targetButton = button;
            cachedButton = button;
        }

        public void SetRepeat(bool shouldRepeat, float interval)
        {
            repeat = shouldRepeat;
            repeatInterval = interval;
            if (isActiveAndEnabled)
            {
                CancelInvoke(nameof(Execute));
                OnEnable();
            }
        }
    }
}
