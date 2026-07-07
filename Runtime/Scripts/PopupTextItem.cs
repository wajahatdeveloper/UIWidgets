using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace UIWidgets
{
    public class PopupTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rectTransform;

        private Coroutine _playCoroutine;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;
            if (text == null)
                text = GetComponentInChildren<TextMeshProUGUI>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Play(
            Vector3 worldPosition,
            string message,
            Color color,
            PopupTextAnimationSettings settings,
            Camera camera,
            Action onComplete)
        {
            StopAndReset();

            if (text != null)
            {
                text.text = message ?? string.Empty;
                text.color = color;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            transform.position = worldPosition;
            gameObject.SetActive(true);
            _playCoroutine = StartCoroutine(PlayRoutine(worldPosition, settings, camera, onComplete));
        }

        public void StopAndReset()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        private IEnumerator PlayRoutine(
            Vector3 startWorldPosition,
            PopupTextAnimationSettings settings,
            Camera camera,
            Action onComplete)
        {
            float lifetime = settings != null ? settings.itemLifetime : 1.5f;
            float riseDistance = settings != null ? settings.riseDistance : 1f;
            float riseSpeed = settings != null ? settings.riseSpeed : 1f;
            float fadeDuration = settings != null ? settings.fadeDuration : 0.5f;
            bool billboard = settings == null || settings.billboard;
            bool useUnscaledTime = settings == null || settings.useUnscaledTime;

            float elapsed = 0f;
            float fadeStart = Mathf.Max(0f, lifetime - fadeDuration);

            while (elapsed < lifetime)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;

                float riseT = Mathf.Clamp01(elapsed * riseSpeed);
                Vector3 position = startWorldPosition + Vector3.up * (riseDistance * riseT);
                transform.position = position;

                if (billboard && camera != null)
                {
                    Vector3 forward = transform.position - camera.transform.position;
                    if (forward.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(forward);
                }

                if (canvasGroup != null && elapsed >= fadeStart && fadeDuration > 0f)
                {
                    float fadeT = Mathf.Clamp01((elapsed - fadeStart) / fadeDuration);
                    canvasGroup.alpha = 1f - fadeT;
                }

                yield return null;
            }

            _playCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
