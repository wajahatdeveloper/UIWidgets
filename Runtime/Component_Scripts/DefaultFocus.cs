using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    public class DefaultFocus : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("UI control to focus when this object is enabled.")]
        [SerializeField] private Selectable controlToFocus;

        [Header("Timing")]
        [Tooltip("Delay before attempting to focus the control.")]
        [SerializeField] private float startDelay = 0.0f;

        [Header("Behavior")]
        [Tooltip("Start focusing automatically on OnEnable.")]
        [SerializeField] private bool focusOnEnable = true;
        [Tooltip("Also set EventSystem.current.selectedGameObject to the control.")]
        [SerializeField] private bool alsoSetEventSystemSelected = true;
        [Tooltip("Only focus if the control is interactable and enabled.")]
        [SerializeField] private bool onlyIfInteractable = true;
        [Tooltip("Max frames to retry while waiting for the control to become valid/active.")]
        [SerializeField] private int maxRetryFrames = 5;

        private Coroutine focusRoutine;

        private void OnEnable()
        {
            if (!focusOnEnable)
            {
                return;
            }

            Focus();
        }

        private void OnDisable()
        {
            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }
        }

        public void Focus()
        {
            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
            }
            focusRoutine = StartCoroutine(FocusControl());
        }

        private IEnumerator FocusControl()
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            // Wait a frame to ensure UI/layout is ready
            yield return null;

            if (controlToFocus == null)
            {
                yield break;
            }

            int retriesRemaining = Mathf.Max(0, maxRetryFrames);
            while (retriesRemaining-- > 0)
            {
                if (!gameObject.activeInHierarchy)
                {
                    yield break;
                }

                if (controlToFocus.gameObject.activeInHierarchy && controlToFocus.enabled && (!onlyIfInteractable || controlToFocus.interactable))
                {
                    try
                    {
                        controlToFocus.Select();
                    }
                    catch
                    {
                        // Ignore select exceptions and retry next frame
                    }

                    if (alsoSetEventSystemSelected && EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(controlToFocus.gameObject);
                    }
                    break;
                }

                yield return null;
            }

            focusRoutine = null;
        }
    }
}
