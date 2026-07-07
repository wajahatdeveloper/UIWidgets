using System.Collections;
using FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIWidgets
{
    [RequireComponent(typeof(RectTransform))]
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        /// <summary>
        /// Optional provider to supply tooltip text dynamically. If present, it overrides the static text.
        /// Keep everything inside this file so you only need ToolTip + TooltipTrigger.
        /// </summary>
        public interface ITooltipContentProvider
        {
            string GetTooltipText();
        }

        [TextArea]
        public string text;

        public enum TooltipPositioningType {
            mousePosition,
            mousePositionAndFollow,
            transformPosition
        }

        [Tooltip("Defines where the tooltip will be placed and how that placement will occur. Transform position will always be used if this element wasn't selected via mouse")]
        public TooltipPositioningType tooltipPositioningType = TooltipPositioningType.mousePosition;

        /// <summary>
        /// This info is needed to make sure we make the necessary translations if the tooltip and this trigger are children of different space canvases
        /// </summary>
        private bool isChildOfOverlayCanvas = false;

        private bool hovered = false;

        public Vector3 offset;

		[Tooltip("When using mouse-based positioning, take the raw PointerEventData.position on enter.")]
		public bool usePointerEventPosition = false;

        [Header("Delays (seconds)")]
        [Tooltip("Delay before showing the tooltip after hover/select.")]
        public float showDelay = 0f;

        [Tooltip("Delay before hiding the tooltip after exit/deselect.")]
		public float hideDelay = 0.1f;

        [Header("Advanced")]
        [Tooltip("If true and a component implements ITooltipContentProvider, it will be used for text.")]
        public bool useContentProviderIfAvailable = true;

        private Coroutine showRoutine;
        private Coroutine hideRoutine;
        private Coroutine followRoutine;

        private ToolTip _toolTip;


        void Start() {
            //attempt to check if our canvas is overlay or not and check our "is overlay" accordingly
            Canvas ourCanvas = GetComponentInParent<Canvas>();
            if (ourCanvas && ourCanvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                isChildOfOverlayCanvas = true;
            }

            _toolTip = ToolTip.Instance;
            if (_toolTip == null) {
                DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:TooltipTrigger] No ToolTip found in scene. Tooltips will be disabled for this trigger.");
            }
        }

        /// <summary>
        /// Checks if the tooltip and the transform this trigger is attached to are children of differently-spaced Canvases
        /// </summary>
        public bool WorldToScreenIsRequired
        {
            get
            {
                if (_toolTip == null) return false;
                return (isChildOfOverlayCanvas && _toolTip.guiMode == RenderMode.ScreenSpaceCamera) ||
                    (!isChildOfOverlayCanvas && _toolTip.guiMode == RenderMode.ScreenSpaceOverlay);
            }
        }

		public void OnPointerEnter(PointerEventData eventData)
        {
            if (_toolTip == null) return;
            CancelHideRoutine();
            StartShowRoutine(() =>
            {
				switch (tooltipPositioningType) {
					case TooltipPositioningType.mousePosition:
						StartHover((usePointerEventPosition ? (Vector3)eventData.position : UIInput.MousePosition) + offset, true);
						break;
					case TooltipPositioningType.mousePositionAndFollow:
						StartHover((usePointerEventPosition ? (Vector3)eventData.position : UIInput.MousePosition) + offset, true);
						hovered = true;
						StartFollowRoutine();
						break;
					case TooltipPositioningType.transformPosition:
						StartHover((WorldToScreenIsRequired ? 
							_toolTip.GuiCamera.WorldToScreenPoint(transform.position) :
							transform.position) + offset, true);
						break;
				}
            });
        }

        IEnumerator HoveredMouseFollowingLoop() {
            while (hovered) {
                StartHover(UIInput.MousePosition + offset);
                yield return null;
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_toolTip == null) return;
            CancelHideRoutine();
            StartShowRoutine(() =>
            {
                StartHover((WorldToScreenIsRequired ? 
                    _toolTip.GuiCamera.WorldToScreenPoint(transform.position) :
                            transform.position) + offset, true);
            });
        }

		public void OnPointerExit(PointerEventData eventData)
        {
			if (PointerOverTooltip(eventData))
			{
				return;
			}
			BeginHide();
        }

		public void OnDeselect(BaseEventData eventData)
        {
			BeginHide();
        }

        void StartHover(Vector3 position, bool shouldCanvasUpdate = false)
        {
            if (_toolTip == null) return;
            string content = GetTooltipText();
            _toolTip.SetTooltip(content, position, shouldCanvasUpdate);
        }

        void StopHover()
        {
            hovered = false;
            if (followRoutine != null)
            {
                StopCoroutine(followRoutine);
                followRoutine = null;
            }
            if (_toolTip == null) return;
            _toolTip.HideTooltip();
        }

        void StartShowRoutine(System.Action onShown)
        {
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
            showRoutine = StartCoroutine(ShowAfterDelay(onShown));
        }

        IEnumerator ShowAfterDelay(System.Action onShown)
        {
            if (showDelay > 0f)
            {
                yield return new WaitForSeconds(showDelay);
            }
            onShown?.Invoke();
            showRoutine = null;
        }

        void BeginHide()
        {
            hovered = false;
            CancelShowRoutine();
            if (followRoutine != null)
            {
                StopCoroutine(followRoutine);
                followRoutine = null;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

		IEnumerator HideAfterDelay()
        {
            if (hideDelay > 0f)
            {
                yield return new WaitForSeconds(hideDelay);
            }
            StopHover();
            hideRoutine = null;
        }

		bool PointerOverTooltip(PointerEventData eventData)
		{
			var tt = ToolTip.Instance;
			if (tt == null || !tt.gameObject.activeInHierarchy) return false;
			var r = tt.GetComponent<RectTransform>();
			if (r == null) return false;
			return RectTransformUtility.RectangleContainsScreenPoint(r, eventData.position, tt.GuiCamera);
		}

        void CancelShowRoutine()
        {
            if (showRoutine != null)
            {
                StopCoroutine(showRoutine);
                showRoutine = null;
            }
        }

        void CancelHideRoutine()
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
        }

        void StartFollowRoutine()
        {
            if (followRoutine != null)
            {
                StopCoroutine(followRoutine);
            }
            followRoutine = StartCoroutine(HoveredMouseFollowingLoop());
        }

        string GetTooltipText()
        {
            if (useContentProviderIfAvailable)
            {
                var provider = GetComponent<ITooltipContentProvider>();
                if (provider != null)
                {
                    var provided = provider.GetTooltipText();
                    if (!string.IsNullOrEmpty(provided)) return provided;
                }
            }
            return text;
        }
    }
}
