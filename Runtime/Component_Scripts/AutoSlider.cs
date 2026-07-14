using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    public class AutoSlider : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Slider slider;

        [Header("Behavior")]
        [Tooltip("Automatically start filling on Start().")]
        [SerializeField] private bool autoStart = false;
        [Tooltip("Start from zero when starting.")]
        [SerializeField] private bool resetToZeroOnStart = true;
        [Tooltip("Stop and reset when disabled.")]
        [SerializeField] private bool resetOnDisable = false;

        [Header("Timing")]
        [Tooltip("Seconds to wait per increment step (realtime).")]
        [SerializeField] private float waitTimePerLoop = 0.1f;
        [Tooltip("Value added per step [0..1].")]
        [SerializeField] private float fillValuePerLoop = 0.03f;

        [Header("Events")]
        [SerializeField] private UnityEvent onStartFill;
        [SerializeField] private UnityEvent<float> onValueChanged;
        [SerializeField] private UnityEvent onCompleted;

        private Coroutine runRoutine;
        private bool isPaused;

        [Header("Snapping (Optional)")]
        [Tooltip("If > 0, values are snapped to this step size (e.g., 0.05).")]
        [SerializeField] private float snapStep = 0f;

        [Header("Safety (Optional)")]
        [Tooltip("If > 0, auto stops after this many seconds (realtime) to avoid infinite runs.")]
        [SerializeField] private float maxRunSeconds = 0f;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        private void Start()
        {
            if (slider != null && resetToZeroOnStart)
            {
                slider.value = 0.0f;
                onValueChanged?.Invoke(slider.value);
            }

            if (autoStart)
            {
                StartAutoSlider();
            }
        }

        private void OnDisable()
        {
            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
                runRoutine = null;
            }

            if (resetOnDisable && slider != null)
            {
                slider.value = 0.0f;
                onValueChanged?.Invoke(slider.value);
            }
        }

        public void StartAutoSlider()
        {
            if (slider == null)
            {
                return;
            }

            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
            }

            onStartFill?.Invoke();
            runRoutine = StartCoroutine(RunSlider());
        }

        public void StopAutoSlider()
        {
            if (runRoutine != null)
            {
                StopCoroutine(runRoutine);
                runRoutine = null;
            }
            isPaused = false;
        }

        public IEnumerator RunSlider()
        {
            if (slider == null)
            {
                yield break;
            }

            float startTime = Time.realtimeSinceStartup;
            while (slider.value < 1.0f)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, waitTimePerLoop));

                if (slider == null)
                {
                    yield break;
                }

                float increment = Mathf.Max(0f, fillValuePerLoop);
                float newValue = Mathf.Clamp01(slider.value + increment);
                if (snapStep > 0f)
                {
                    newValue = Mathf.Round(newValue / snapStep) * snapStep;
                    newValue = Mathf.Clamp01(newValue);
                }
                if (!Mathf.Approximately(newValue, slider.value))
                {
                    slider.value = newValue;
                    onValueChanged?.Invoke(newValue);
                }

                if (Mathf.Approximately(slider.value, 1.0f))
                {
                    break;
                }

                if (maxRunSeconds > 0f && (Time.realtimeSinceStartup - startTime) >= maxRunSeconds)
                {
                    break;
                }
            }

            onCompleted?.Invoke();
            runRoutine = null;
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Resume()
        {
            isPaused = false;
        }

        public bool IsRunning()
        {
            return runRoutine != null && !isPaused;
        }
    }
}
