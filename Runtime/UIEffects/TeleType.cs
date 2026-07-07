using System.Collections;
using FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;

namespace UIWidgets
{
    public class TeleType : MonoBehaviour
    {
        [Header("Typing Settings")]
        public bool onlyOnce;
        public bool playOnEnable = true;
        [Tooltip("Characters per second. Set higher for faster typing.")]
        public float charactersPerSecond = 20f;
        [Tooltip("Delay before typing starts.")]
        public float startDelay = 0f;
        [Tooltip("Delay after finishing before looping or stopping.")]
        public float endHoldDuration = 1f;
        [Tooltip("Use unscaled time for UI typing.")]
        public bool useUnscaledTime = true;

        private TextMeshProUGUI _textMeshProUGUI;
        private Coroutine _typingCoroutine;
        private string _originalText;

        private void Awake()
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_textMeshProUGUI == null)
            {
                _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
                if (_textMeshProUGUI == null)
                    DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:TeleType] {Name}: TextMeshProUGUI is null. Assign or ensure component exists.", name);
            }
            _originalText = _textMeshProUGUI != null ? _textMeshProUGUI.text : string.Empty;
            if (_textMeshProUGUI != null)
            {
                _textMeshProUGUI.maxVisibleCharacters = 0;
            }
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
            if (_textMeshProUGUI != null)
            {
                _textMeshProUGUI.maxVisibleCharacters = 0;
            }
        }

        public void Play(string overrideText = null)
        {
            if (_textMeshProUGUI == null)
            {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:TeleType] {Name}: TextMeshProUGUI is null. Cannot play.", name);
                return;
            }
            if (overrideText != null)
            {
                _textMeshProUGUI.text = overrideText;
            }
            if (_typingCoroutine != null) { StopCoroutine(_typingCoroutine); }
            _typingCoroutine = StartCoroutine(TypeRoutine());
        }

        public void Stop()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
        }

        public void SetTextAndPlay(string newText)
        {
            _originalText = newText;
            Play(newText);
        }

        private IEnumerator TypeRoutine()
        {
            // Ensure layout and text info are up to date
            _textMeshProUGUI.ForceMeshUpdate();
            int totalVisibleCharacters = _textMeshProUGUI.textInfo.characterCount;
            _textMeshProUGUI.maxVisibleCharacters = 0;

            // Start delay
            if (startDelay > 0f)
            {
                if (useUnscaledTime) { yield return new WaitForSecondsRealtime(startDelay); }
                else { yield return new WaitForSeconds(startDelay); }
            }

            if (charactersPerSecond <= 0f) { charactersPerSecond = 1f; }
            float secondsPerChar = 1f / charactersPerSecond;
            int counter = 0;

            while (true)
            {
                int visibleCount = Mathf.Clamp(counter, 0, totalVisibleCharacters);
                _textMeshProUGUI.maxVisibleCharacters = visibleCount;

                if (visibleCount >= totalVisibleCharacters)
                {
                    if (onlyOnce)
                    {
                        _typingCoroutine = null;
                        yield break;
                    }
                    else
                    {
                        if (endHoldDuration > 0f)
                        {
                            if (useUnscaledTime) { yield return new WaitForSecondsRealtime(endHoldDuration); }
                            else { yield return new WaitForSeconds(endHoldDuration); }
                        }
                        counter = 0;
                        _textMeshProUGUI.maxVisibleCharacters = 0;
                        // Recompute in case text changed externally
                        _textMeshProUGUI.ForceMeshUpdate();
                        totalVisibleCharacters = _textMeshProUGUI.textInfo.characterCount;
                    }
                }
                else
                {
                    counter += 1;
                    if (useUnscaledTime) { yield return new WaitForSecondsRealtime(secondsPerChar); }
                    else { yield return new WaitForSeconds(secondsPerChar); }
                }
            }
        }
    }
}
