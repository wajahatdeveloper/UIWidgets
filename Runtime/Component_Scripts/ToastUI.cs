using System;
using System.Collections;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
   public class ToastUI : PersistentSingletonBehaviour<ToastUI> {
      [Header ("UI References :")]
      [SerializeField] private CanvasGroup uiCanvasGroup ;
      [SerializeField] private RectTransform uiRectTransform ;
      [SerializeField] private VerticalLayoutGroup uiContentVerticalLayoutGroup ;
      [SerializeField] private Image uiImage ;
      [SerializeField] private TextMeshProUGUI uiText ;
      [SerializeField] private Button clickButton ;

      [Header ("Toast Colors :")]
      [Tooltip("Optional shared palette. When assigned, overrides the inline Colors array for ToastColor lookups.")]
      [SerializeField] private ToastColorPalette palette ;
      [SerializeField] private Color[] colors ;

      [Header ("Toast Fade In/Out Duration :")]
      [Range (.05f, 2f)]
      [SerializeField] private float fadeDuration = .3f ;
      [Tooltip("Use unscaled time for UI fade and wait.")]
      [SerializeField] private bool useUnscaledTime = true ;

      [Header ("Positioning :")]
      [SerializeField] private float positionOffset = 20f ;

      [Tooltip("Longer text is truncated with an ellipsis.")]
      [SerializeField] private int maxTextLength = 300 ;
      private bool _clickToDismiss = false;
      private Action _onDismiss = null;
      private Coroutine _currentFadeCoroutine;

      protected override void Awake () {
         base.Awake () ;
         if (uiCanvasGroup == null) { uiCanvasGroup = GetComponentInChildren<CanvasGroup>(); }
         if (clickButton == null) { clickButton = GetComponent<Button>(); }

         if (uiCanvasGroup != null) {
            uiCanvasGroup.alpha = 0f ;
         } else {
            DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] ToastUI: uiCanvasGroup reference is missing; fade will not work.");
         }
         
         // Setup click button
         if (clickButton != null) {
            clickButton.onClick.AddListener(OnClickToDismiss);
         }
      }

      public void Init (string text, float duration, ToastColor color, ToastPosition position) {
         Show (text, duration, ResolveColor (color), position, false, null) ;
      }

      public void Init (string text, float duration, Color color, ToastPosition position) {
         Show (text, duration, color, position, false, null) ;
      }

      public void Init (string text, float duration, ToastColor color, ToastPosition position, bool clickToDismiss, Action onDismiss) {
         Show (text, duration, ResolveColor (color), position, clickToDismiss, onDismiss) ;
      }

      public void Init (string text, float duration, Color color, ToastPosition position, bool clickToDismiss, Action onDismiss) {
         Show (text, duration, color, position, clickToDismiss, onDismiss) ;
      }

      private Color ResolveColor (ToastColor color) {
         int i = (int)color ;
         if (palette != null && i >= 0 && i < palette.Count)
            return palette.GetColor (i) ;
         return (colors != null && i >= 0 && i < colors.Length) ? colors [i] : Color.white ;
      }

      private void Show (string text, float duration, Color color, ToastPosition position, bool clickToDismiss, Action onDismiss) {
         if (uiText != null) {
            uiText.text = (!string.IsNullOrEmpty(text) && text.Length > maxTextLength) ? text.Substring (0, maxTextLength) + "..." : text ;
         }
         if (uiImage != null) { uiImage.color = color ; }

         // Setup positioning
         SetupPosition(position);

         // Setup click to dismiss
         _clickToDismiss = clickToDismiss;
         if (clickButton != null) {
            clickButton.enabled = clickToDismiss;
         }

         // Reset any prior state/coroutine and clear any stale callback before assigning the new one.
         Dismiss () ;
         _onDismiss = onDismiss;
         _currentFadeCoroutine = StartCoroutine (FadeInOut (duration, fadeDuration)) ;
      }

      private void SetupPosition(ToastPosition position) {
         if (uiContentVerticalLayoutGroup != null) {
            uiContentVerticalLayoutGroup.childAlignment = (TextAnchor)((int)position) ;
         }

         // Apply position offset for better visual spacing
         if (uiRectTransform != null) {
            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.zero;
            Vector2 offsetMin = Vector2.zero;
            Vector2 offsetMax = Vector2.zero;

            switch (position) {
               case ToastPosition.TopLeft:
                  anchorMin = new Vector2(0, 1);
                  anchorMax = new Vector2(0, 1);
                  offsetMin = new Vector2(positionOffset, -positionOffset);
                  offsetMax = new Vector2(positionOffset, -positionOffset);
                  break;
               case ToastPosition.TopCenter:
                  anchorMin = new Vector2(0.5f, 1);
                  anchorMax = new Vector2(0.5f, 1);
                  offsetMin = new Vector2(-positionOffset, -positionOffset);
                  offsetMax = new Vector2(positionOffset, -positionOffset);
                  break;
               case ToastPosition.TopRight:
                  anchorMin = new Vector2(1, 1);
                  anchorMax = new Vector2(1, 1);
                  offsetMin = new Vector2(-positionOffset, -positionOffset);
                  offsetMax = new Vector2(-positionOffset, -positionOffset);
                  break;
               case ToastPosition.MiddleLeft:
                  anchorMin = new Vector2(0, 0.5f);
                  anchorMax = new Vector2(0, 0.5f);
                  offsetMin = new Vector2(positionOffset, -positionOffset);
                  offsetMax = new Vector2(positionOffset, positionOffset);
                  break;
               case ToastPosition.MiddleCenter:
                  anchorMin = new Vector2(0.5f, 0.5f);
                  anchorMax = new Vector2(0.5f, 0.5f);
                  offsetMin = new Vector2(-positionOffset, -positionOffset);
                  offsetMax = new Vector2(positionOffset, positionOffset);
                  break;
               case ToastPosition.MiddleRight:
                  anchorMin = new Vector2(1, 0.5f);
                  anchorMax = new Vector2(1, 0.5f);
                  offsetMin = new Vector2(-positionOffset, -positionOffset);
                  offsetMax = new Vector2(-positionOffset, positionOffset);
                  break;
               case ToastPosition.BottomLeft:
                  anchorMin = new Vector2(0, 0);
                  anchorMax = new Vector2(0, 0);
                  offsetMin = new Vector2(positionOffset, positionOffset);
                  offsetMax = new Vector2(positionOffset, positionOffset);
                  break;
               case ToastPosition.BottomCenter:
                  anchorMin = new Vector2(0.5f, 0);
                  anchorMax = new Vector2(0.5f, 0);
                  offsetMin = new Vector2(-positionOffset, positionOffset);
                  offsetMax = new Vector2(positionOffset, positionOffset);
                  break;
               case ToastPosition.BottomRight:
                  anchorMin = new Vector2(1, 0);
                  anchorMax = new Vector2(1, 0);
                  offsetMin = new Vector2(-positionOffset, positionOffset);
                  offsetMax = new Vector2(-positionOffset, positionOffset);
                  break;
            }

            uiRectTransform.anchorMin = anchorMin;
            uiRectTransform.anchorMax = anchorMax;
            uiRectTransform.offsetMin = offsetMin;
            uiRectTransform.offsetMax = offsetMax;
         }
      }

      private void OnClickToDismiss() {
         if (_clickToDismiss) {
            Dismiss();
         }
      }

      private IEnumerator FadeInOut (float toastDuration, float fadeDuration) {
         yield return null ;
         if (uiContentVerticalLayoutGroup != null) {
            uiContentVerticalLayoutGroup.CalculateLayoutInputHorizontal () ;
            uiContentVerticalLayoutGroup.CalculateLayoutInputVertical () ;
            uiContentVerticalLayoutGroup.SetLayoutHorizontal () ;
            uiContentVerticalLayoutGroup.SetLayoutVertical () ;
         }
         yield return null ;
         // Anim start
         yield return Fade (uiCanvasGroup, 0f, 1f, fadeDuration) ;
         if (useUnscaledTime) { yield return new WaitForSecondsRealtime (toastDuration) ; }
         else { yield return new WaitForSeconds (toastDuration) ; }
         yield return Fade (uiCanvasGroup, 1f, 0f, fadeDuration) ;
         // Anim end
         
         // Call dismiss callback
         InvokeDismissOnce();
      }

      private void InvokeDismissOnce() {
         var callback = _onDismiss;
         _onDismiss = null;
         callback?.Invoke();
      }

      private IEnumerator Fade (CanvasGroup cGroup, float startAlpha, float endAlpha, float fadeDuration) {
         float startTime = useUnscaledTime ? Time.unscaledTime : Time.time ;
         float alpha = startAlpha ;

         if (fadeDuration > 0f) {
            //Anim start
            while (alpha != endAlpha) {
               float t = ((useUnscaledTime ? Time.unscaledTime : Time.time) - startTime) / fadeDuration ;
               alpha = Mathf.Lerp (startAlpha, endAlpha, t) ;
               cGroup.alpha = alpha ;

               yield return null ;
            }
         }

         cGroup.alpha = endAlpha ;
      }

      public void Dismiss () {
         if (_currentFadeCoroutine != null) {
            StopCoroutine(_currentFadeCoroutine);
            _currentFadeCoroutine = null;
         }
         if (uiCanvasGroup != null) { uiCanvasGroup.alpha = 0f ; }

         // Call dismiss callback
         InvokeDismissOnce();
      }

      protected override void OnDestroy () {
         base.OnDestroy () ;
      }
   }
}