using System.Collections;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	public class Fader : SingletonBehaviour<Fader>
	{
		public Image fadeImage;
		[Space]
		public UnityEvent OnFadeToBlackComplete;
		public UnityEvent OnFadeFromBlackComplete;
		[Space]
		[Tooltip("Base color to use while fading. Alpha will be animated.")]
		public Color fadeColor = Color.black;
		[Tooltip("Animation curve used to ease the fade (0..1 time -> 0..1 alpha).")]
		public AnimationCurve easing = AnimationCurve.Linear(0, 0, 1, 1);
		[Tooltip("Use unscaled time (ignores Time.timeScale) for the fade.")]
		public bool useUnscaledTime = true;
		[Tooltip("When enabled, the fade image will block raycasts while fading and when alpha > 0.")]
		public bool blockRaycastsDuringFade = true;

		private bool isFadeToBlack = false;
		private bool isFading = false;
		private Coroutine fadeRoutine;
		private readonly Color _opaqueBlack = new Color(0f, 0f, 0f, 1f);

		[ContextMenu(nameof(FadeToBlack))]
		public void FadeToBlack(float duration = 1.0f)
		{
			BeginFade(true, duration);
		}

		[ContextMenu(nameof(FadeFromBlack))]
		public void FadeFromBlack(float duration = 1.0f)
		{
			BeginFade(false, duration);
		}

		/// <summary>
		/// Cancel the current fade, if any, leaving the image at its current alpha.
		/// </summary>
		public void CancelCurrentFade()
		{
			if (fadeRoutine != null)
			{
				StopCoroutine(fadeRoutine);
				fadeRoutine = null;
				isFading = false;
				
				// Disable the fade image if alpha is 0 when cancelling
				if (fadeImage != null && fadeImage.color.a <= 0f)
				{
					fadeImage.gameObject.SetActive(false);
				}
			}
		}

		public bool IsFading => isFading;
		public float CurrentAlpha => fadeImage != null ? fadeImage.color.a : 0f;

		protected new void Awake()
		{
			base.Awake();
			if (fadeImage == null)
			{
				fadeImage = GetComponentInChildren<Image>(true);
			}
			EnsureImageConfigured();
		}

		private void OnDisable()
		{
			isFading = false;
			fadeRoutine = null;
			
			// Disable the fade image when the component is disabled
			if (fadeImage != null)
			{
				fadeImage.gameObject.SetActive(false);
			}
		}

		private void BeginFade(bool toBlack, float duration)
		{
			if (fadeImage == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Panel] Fader: no fadeImage assigned or found.");
				return;
			}

			isFadeToBlack = toBlack;

			if (fadeRoutine != null)
			{
				StopCoroutine(fadeRoutine);
			}

			// Enable the fade image when starting a fade
			fadeImage.gameObject.SetActive(true);

			if (duration <= 0f)
			{
				SetAlpha(toBlack ? 1f : 0f);
				
				// Disable the fade image if alpha is 0 for instant fade
				if (!toBlack && fadeImage != null)
				{
					fadeImage.gameObject.SetActive(false);
				}
				
				InvokeCompletion();
				return;
			}

			isFading = true;
			fadeRoutine = StartCoroutine(Fade(toBlack ? 0f : 1f, toBlack ? 1f : 0f, duration));
		}

		private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
		{
			EnsureImageConfigured();

			if (blockRaycastsDuringFade)
			{
				fadeImage.raycastTarget = true;
			}

			float t = 0f;
			while (t < duration)
			{
				float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
				t += dt;
				float normalizedTime = Mathf.Clamp01(t / duration);
				float eased = easing != null ? easing.Evaluate(normalizedTime) : normalizedTime;
				SetAlpha(Mathf.Lerp(startAlpha, endAlpha, eased));
				yield return null;
			}

			SetAlpha(endAlpha); // ensure exact final value

			isFading = false;
			if (blockRaycastsDuringFade)
			{
				fadeImage.raycastTarget = endAlpha > 0f;
			}

			// Disable the fade image when fade is complete and alpha is 0
			if (endAlpha <= 0f)
			{
				fadeImage.gameObject.SetActive(false);
			}

			InvokeCompletion();
		}

		private void SetAlpha(float alpha)
		{
			if (fadeImage == null) return;
			Color c = fadeColor;
			c.a = Mathf.Clamp01(alpha);
			fadeImage.color = c;
		}

		private void EnsureImageConfigured()
		{
			if (fadeImage == null) return;
			// Ensure the RGB of the image matches fadeColor's RGB, keep current alpha
			Color current = fadeImage.color;
			fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, current.a);
		}

		private void InvokeCompletion()
		{
			if (isFadeToBlack)
			{
				OnFadeToBlackComplete?.Invoke();
			}
			else
			{
				OnFadeFromBlackComplete?.Invoke();
			}
		}
	}
}