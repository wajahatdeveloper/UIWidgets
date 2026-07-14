using System;
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIWidgets
{
    /// <summary>
    /// A component to identify and interfere with Panel gameObjects.
    /// </summary>
    public class PanelBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public event Action OnBeforeShown;
        public event Action OnAfterShown;

        public event Action OnBeforeHidden;
        public event Action OnAfterHidden;

        protected virtual int ChildIndex => -1;

        [Header("Modal")]
        [Tooltip("When true, Show()/Hide() route through ModalService: only one modal is visible at a time, " +
                 "an input-blocking backdrop is raised behind it, and it is sorted above base UI. " +
                 "Non-modal panels behave as a plain activate/deactivate.")]
        [SerializeField] private bool isModal = false;

        /// <summary>Whether this panel participates in <see cref="ModalService"/> coordination.</summary>
        public bool IsModal => isModal;

        public bool IsShown()
        {
            return gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Shows the panel. Modal panels route through <see cref="ModalService"/> (single active
        /// modal + backdrop + sort order); non-modal panels activate directly.
        /// </summary>
        public void Show()
        {
            if (isModal && ModalService.HasInstance)
            {
                ModalService.Instance.Show(this);
                return;
            }
            ShowInternal();
        }

        /// <summary>
        /// Activates the panel <see cref="GameObject"/> or its child at ChildIndex, firing the
        /// before/after-show hooks. Called directly for non-modal panels, or by
        /// <see cref="ModalService"/> for modal panels.
        /// </summary>
        internal void ShowInternal()
        {
            OnBeforeShow();
            if (ChildIndex == -1)
            {
                gameObject.SetActive(true);
            }
            else
            {
			if (ChildIndex >= 0 && ChildIndex < transform.childCount)
			{
				transform.GetChild(ChildIndex).gameObject.SetActive(true);
			}
			else
			{
				DebugX.Builder(LogChannels.UI).WithContext(gameObject).Warning("PanelBase.Show: _childIndex {ChildIndex} is out of range for {GameObjectName}", ChildIndex, gameObject.name);
			}
            }
            OnAfterShow();
        }

        /// <summary>
        /// Hides the panel. Modal panels that are the current modal route through
        /// <see cref="ModalService"/>; everything else deactivates directly.
        /// </summary>
        public void Hide()
        {
            if (isModal && ModalService.HasInstance && ModalService.Instance.Current == this)
            {
                ModalService.Instance.HideCurrent();
                return;
            }
            HideInternal();
        }

        /// <summary>
        /// Deactivates the panel <see cref="GameObject"/> or its child at ChildIndex, firing the
        /// before/after-hide hooks. Called directly for non-modal panels, or by
        /// <see cref="ModalService"/> for modal panels.
        /// </summary>
        internal void HideInternal()
        {
            OnBeforeHide();
            if (ChildIndex == -1)
            {
                gameObject.SetActive(false);
            }
            else
            {
			if (ChildIndex >= 0 && ChildIndex < transform.childCount)
			{
				transform.GetChild(ChildIndex).gameObject.SetActive(false);
			}
			else
			{
				DebugX.Builder(LogChannels.UI).WithContext(gameObject).Warning("PanelBase.Hide: _childIndex {ChildIndex} is out of range for {GameObjectName}", ChildIndex, gameObject.name);
			}
            }
            OnAfterHide();
        }

        // Dragging options
        [Tooltip("Enable dragging this panel within its parent Canvas bounds.")]
        public bool Draggable = false;

        [Tooltip("Number of pixels of the panel that must stay inside the canvas view when dragging.")]
        public int KeepWindowInCanvas = 5;

        [Tooltip("The transform that is moved when dragging. If empty, uses this object's RectTransform.")]
        public RectTransform RootTransform = null;

        private bool _isDragging = false;
        private Canvas _canvas;
        private RectTransform _canvasRectTransform;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            // Modal and Draggable are mutually exclusive: a modal is shown one-at-a-time, fixed
            // behind its input-blocking backdrop. Modal wins the conflict — a draggable modal is
            // the unsupported case (its own Canvas would fight the one ModalService adds at
            // show-time). The custom PanelBaseEditor also auto-clears one when the other is set;
            // this is the backstop for values coming from prefabs or script.
            if (isModal && Draggable)
            {
                Draggable = false;
            }
        }
#endif

        void Start()
        {
            if (!Draggable)
            {
                return;
            }

            if (RootTransform == null)
            {
                RootTransform = GetComponent<RectTransform>();
            }

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                _canvasRectTransform = _canvas.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Called just before showing panel.
        /// </summary>
        protected virtual void OnBeforeShow()
        {
            OnBeforeShown?.Invoke();
        }

        /// <summary>
        /// Called just after showing panel.
        /// </summary>
		protected virtual void OnAfterShow()
		{
			DebugX.Builder(LogChannels.UI).WithContext(gameObject).Info("{GameObjectName} : Shown", gameObject.name);
			OnAfterShown?.Invoke();
		}

        /// <summary>
        /// Called just before hiding panel.
        /// </summary>
        protected virtual void OnBeforeHide()
        {
            OnBeforeHidden?.Invoke();
        }

        /// <summary>
        /// Called just after hiding panel.
        /// </summary>
        protected virtual void OnAfterHide()
        {
            OnAfterHidden?.Invoke();
        }

        // Drag handlers
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Draggable)
            {
                return;
            }

            if (eventData.pointerCurrentRaycast.gameObject == null)
            {
                return;
            }

            if (eventData.pointerCurrentRaycast.gameObject.name == name)
            {
                _isDragging = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!Draggable || !_isDragging || RootTransform == null || _canvasRectTransform == null)
            {
                return;
            }

            var delta = ScreenToCanvas(eventData.position) - ScreenToCanvas(eventData.position - eventData.delta);
            RootTransform.localPosition += delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!Draggable)
            {
                return;
            }
            _isDragging = false;
        }

        private Vector3 ScreenToCanvas(Vector3 screenPosition)
        {
            if (_canvas == null || _canvasRectTransform == null)
            {
                return screenPosition;
            }

            Vector3 localPosition;
            Vector2 min;
            Vector2 max;
            var canvasSize = _canvasRectTransform.sizeDelta;

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay || (_canvas.renderMode == RenderMode.ScreenSpaceCamera && _canvas.worldCamera == null))
            {
                localPosition = screenPosition;

                min = Vector2.zero;
                max = canvasSize;
            }
            else
            {
                var ray = _canvas.worldCamera.ScreenPointToRay(screenPosition);
                var plane = new Plane(_canvasRectTransform.forward, _canvasRectTransform.position);

                float distance;
                if (!plane.Raycast(ray, out distance))
                {
                    return RootTransform.localPosition;
                }
                var worldPosition = ray.origin + ray.direction * distance;
                localPosition = _canvasRectTransform.InverseTransformPoint(worldPosition);

                min = -Vector2.Scale(canvasSize, _canvasRectTransform.pivot);
                max = Vector2.Scale(canvasSize, Vector2.one - _canvasRectTransform.pivot);
            }

            // keep panel inside canvas
            localPosition.x = Mathf.Clamp(localPosition.x, min.x + KeepWindowInCanvas, max.x - KeepWindowInCanvas);
            localPosition.y = Mathf.Clamp(localPosition.y, min.y + KeepWindowInCanvas, max.y - KeepWindowInCanvas);

            return localPosition;
        }
    }
}