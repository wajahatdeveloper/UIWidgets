using System.Collections;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIWidgets
{
    public class LoadingPanel : SingletonBehaviour<LoadingPanel>
    {
        public GameObject loadingPanel;
        public TextMeshProUGUI infoText;
        public GameObject loadingBar;
        public Image loadingBarImage;

        private static int _counter = 0;
        private Coroutine _timerRoutine;

        public float loadingPercentage
        {
            get { return loadingBarImage != null ? loadingBarImage.fillAmount * 100.0f : 0f; }
            set
            {
                if (loadingBarImage != null)
                {
                    loadingBarImage.fillAmount = Mathf.Clamp01(value / 100.0f);
                }
            }
        }

        public UnityEvent onClose;

        // Fluent API Builder
        public static LoadingPanelBuilder Create(string text = "Loading..")
        {
            return new LoadingPanelBuilder(text);
        }

        // Fluent API Builder Class
        public class LoadingPanelBuilder
        {
            private string _text;
            private bool _showLoadingBar = false;
            private float _timerDuration = 0f;

            internal LoadingPanelBuilder(string text)
            {
                _text = text;
            }

            public LoadingPanelBuilder WithProgressBar(bool show = true)
            {
                _showLoadingBar = show;
                return this;
            }

            public LoadingPanelBuilder WithTimer(float duration)
            {
                _timerDuration = duration;
                return this;
            }

            public void Show()
            {
                Instance.Show(_text, _showLoadingBar);
                if (_timerDuration > 0f)
                {
                    Instance.StartLoadingWithTime(_timerDuration);
                }
            }
        }

        public void Show(string text = "Loading..", bool showLoadingBar = false)
        {
            if (infoText != null)
            {
                infoText.text = text;
            }
            if (loadingBar != null)
            {
                loadingBar.SetActive(showLoadingBar);
            }
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] LoadingPanel Shown {SceneName} id:{Counter}", SceneManager.GetActiveScene().name, _counter);

            _counter++;
        }

        public void StartLoadingWithTime(float time)
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
            }
            _timerRoutine = StartCoroutine(TimerLoading(time));
        }

        private IEnumerator TimerLoading(float time)
        {
            if (loadingBarImage != null)
            {
                loadingBarImage.fillAmount = 0f;
            }
            float val = time;
            while (time >= 0)
            {
                float adjust = time / val;
                if (loadingBarImage != null)
                {
                    loadingBarImage.fillAmount = 1.0f - adjust;
                }
                time -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            _timerRoutine = null;
        }

        public void Hide()
        {
            DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] LoadingPanel Hidden {SceneName} id:{Counter}", SceneManager.GetActiveScene().name, _counter);

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
            onClose?.Invoke();
            onClose?.RemoveAllListeners();

            _counter--;
        }

        public void HideIfShown()
        {
            if (loadingPanel != null && loadingPanel.activeSelf)
            {
                Hide();
            }
        }
    }
}