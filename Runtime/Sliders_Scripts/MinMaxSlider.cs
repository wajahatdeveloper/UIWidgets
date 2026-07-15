using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Dual-handle slider that selects a [min..max] window out of a limit range.
    /// Dragging a handle moves one end of the window; dragging the middle region
    /// shifts the whole window. Vertical drags are forwarded to the nearest parent
    /// drag handler so the slider stays usable inside scroll views.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI (Canvas)/Sliders/MinMax Slider")]
    public class MinMaxSlider : Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Raised when either value changes. Args: (min, max).</summary>
        [Serializable]
        public class SliderEvent : UnityEvent<float, float> { }

        private enum DragTarget
        {
            Window,
            MinHandle,
            MaxHandle
        }

        [Header("UI Controls")]
        [SerializeField] private Camera customCamera;
        [SerializeField] private RectTransform sliderBounds;
        [SerializeField] private RectTransform minHandle;
        [SerializeField] private RectTransform maxHandle;
        [SerializeField] private RectTransform middleGraphic;

        [Header("Display Text (Optional)")]
        [SerializeField] private TextMeshProUGUI minText;
        [SerializeField] private TextMeshProUGUI maxText;
        [SerializeField] private string textFormat = "0";

        [Header("Limits")]
        [SerializeField] private float minLimit = 0f;
        [SerializeField] private float maxLimit = 100f;

        [Header("Values")]
        public bool wholeNumbers;
        [SerializeField] private float minValue = 25f;
        [SerializeField] private float maxValue = 75f;

        public SliderEvent onValueChanged = new SliderEvent();

        public MinMaxValues Values => new MinMaxValues(minValue, maxValue, minLimit, maxLimit);

        public RectTransform SliderBounds { get => sliderBounds; set => sliderBounds = value; }
        public RectTransform MinHandle { get => minHandle; set => minHandle = value; }
        public RectTransform MaxHandle { get => maxHandle; set => maxHandle = value; }
        public RectTransform MiddleGraphic { get => middleGraphic; set => middleGraphic = value; }
        public TextMeshProUGUI MinText { get => minText; set => minText = value; }
        public TextMeshProUGUI MaxText { get => maxText; set => maxText = value; }

        private Vector2 _dragOrigin;
        private float _dragOriginMin01;
        private float _dragOriginMax01;
        private DragTarget _dragTarget;
        private bool _forwardDragToParent;
        // Reused to avoid per-drag allocations while searching for parent handlers.
        private readonly List<Component> _handlerBuffer = new List<Component>();

        private Camera _fallbackCamera;
        private bool _useOverlaySpace;

        protected override void Start()
        {
            base.Start();

            if (sliderBounds == null)
            {
                sliderBounds = transform as RectTransform;
            }

            // A slider instantiated before being parented under a Canvas (pooled
            // prefab setup) has no parent canvas yet; treat that as overlay space
            // rather than dereferencing a missing Canvas.
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            _useOverlaySpace = parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
            _fallbackCamera = customCamera != null ? customCamera : Camera.main;
        }

        private Camera UiCamera => _useOverlaySpace ? null : _fallbackCamera;

        public void SetLimits(float minLimit, float maxLimit)
        {
            this.minLimit = Snap(minLimit);
            this.maxLimit = Snap(maxLimit);
        }

        public void SetValues(MinMaxValues values, bool notify = true)
        {
            SetValues(values.minValue, values.maxValue, values.minLimit, values.maxLimit, notify);
        }

        public void SetValues(float minValue, float maxValue, bool notify = true)
        {
            SetValues(minValue, maxValue, minLimit, maxLimit, notify);
        }

        public void SetValues(float minValue, float maxValue, float minLimit, float maxLimit, bool notify = true)
        {
            this.minValue = Snap(minValue);
            this.maxValue = Snap(maxValue);
            SetLimits(minLimit, maxLimit);

            PositionHandlesFromValues();
            RefreshVisuals();

            if (notify)
            {
                onValueChanged.Invoke(this.minValue, this.maxValue);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // A predominantly vertical gesture belongs to an enclosing scroller.
            _forwardDragToParent = Mathf.Abs(eventData.delta.x) < Mathf.Abs(eventData.delta.y);
            if (_forwardDragToParent)
            {
                ForwardToParent<IBeginDragHandler>(handler => handler.OnBeginDrag(eventData));
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBounds, eventData.position, UiCamera, out _dragOrigin);

            float pressed01 = PointToValue01(_dragOrigin);
            _dragOriginMin01 = GetMin01();
            _dragOriginMax01 = GetMax01();

            if (pressed01 < _dragOriginMin01 || RectTransformUtility.RectangleContainsScreenPoint(minHandle, eventData.position, UiCamera))
            {
                _dragTarget = DragTarget.MinHandle;
                minHandle.SetAsLastSibling();
            }
            else if (pressed01 > _dragOriginMax01 || RectTransformUtility.RectangleContainsScreenPoint(maxHandle, eventData.position, UiCamera))
            {
                _dragTarget = DragTarget.MaxHandle;
                maxHandle.SetAsLastSibling();
            }
            else
            {
                _dragTarget = DragTarget.Window;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_forwardDragToParent)
            {
                ForwardToParent<IDragHandler>(handler => handler.OnDrag(eventData));
                return;
            }

            if (minHandle == null || maxHandle == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderBounds, eventData.position, UiCamera, out Vector2 pointerLocal);
            AnchorHandles();

            switch (_dragTarget)
            {
                case DragTarget.MinHandle:
                    SetMin01(Mathf.Clamp(PointToValue01(pointerLocal), 0f, GetMax01()));
                    break;
                case DragTarget.MaxHandle:
                    SetMax01(Mathf.Clamp(PointToValue01(pointerLocal), GetMin01(), 1f));
                    break;
                default:
                    float shift01 = (pointerLocal.x - _dragOrigin.x) / sliderBounds.rect.width;
                    SetMin01(_dragOriginMin01 + shift01);
                    SetMax01(_dragOriginMax01 + shift01);
                    break;
            }

            // Read values back from the handles positioned above. Calling SetValues
            // here would re-derive handle positions from the values and fight the
            // direct writes, producing visible jitter while dragging.
            minValue = Snap(Mathf.Lerp(minLimit, maxLimit, GetMin01()));
            maxValue = Snap(Mathf.Lerp(minLimit, maxLimit, GetMax01()));

            RefreshVisuals();
            onValueChanged.Invoke(minValue, maxValue);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_forwardDragToParent)
            {
                ForwardToParent<IEndDragHandler>(handler => handler.OnEndDrag(eventData));
                return;
            }

            // When both handles stack on the same extreme, raise the one that can
            // still move inward so the pair cannot get stuck.
            float min01 = GetMin01();
            float max01 = GetMax01();
            if (Mathf.Abs(min01) < MinMaxValues.Tolerance && Mathf.Abs(max01) < MinMaxValues.Tolerance)
            {
                maxHandle.SetAsLastSibling();
            }
            else if (Mathf.Abs(min01 - 1f) < MinMaxValues.Tolerance && Mathf.Abs(max01 - 1f) < MinMaxValues.Tolerance)
            {
                minHandle.SetAsLastSibling();
            }
        }

        private float Snap(float value)
        {
            return wholeNumbers ? Mathf.RoundToInt(value) : value;
        }

        private void PositionHandlesFromValues()
        {
            AnchorHandles();

            float range = maxLimit - minLimit;
            SetMin01((Mathf.Clamp(minValue, minLimit, maxLimit) - minLimit) / range);
            SetMax01((Mathf.Clamp(maxValue, minLimit, maxLimit) - minLimit) / range);
        }

        private void AnchorHandles()
        {
            minHandle.anchorMin = new Vector2(0f, 0.5f);
            minHandle.anchorMax = new Vector2(0f, 0.5f);
            minHandle.pivot = new Vector2(0.5f, 0.5f);

            maxHandle.anchorMin = new Vector2(1f, 0.5f);
            maxHandle.anchorMax = new Vector2(1f, 0.5f);
            maxHandle.pivot = new Vector2(0.5f, 0.5f);
        }

        private void RefreshVisuals()
        {
            if (minText != null)
            {
                minText.SetText(minValue.ToString(textFormat));
            }
            if (maxText != null)
            {
                maxText.SetText(maxValue.ToString(textFormat));
            }

            if (middleGraphic != null)
            {
                middleGraphic.anchorMin = Vector2.zero;
                middleGraphic.anchorMax = Vector2.one;
                middleGraphic.offsetMin = new Vector2(minHandle.anchoredPosition.x, 0f);
                middleGraphic.offsetMax = new Vector2(maxHandle.anchoredPosition.x, 0f);
            }
        }

        // The min handle anchors to the left edge of the bounds, the max handle to
        // the right edge; the conversions below are the two anchor-relative forms
        // of "normalized position along the bounds width".

        private float GetMin01()
        {
            return (minHandle.anchoredPosition.x - sliderBounds.offsetMin.x) / sliderBounds.rect.width;
        }

        private float GetMax01()
        {
            return (maxHandle.anchoredPosition.x - sliderBounds.offsetMax.x) / sliderBounds.rect.width + 1f;
        }

        private void SetMin01(float value01)
        {
            minHandle.anchoredPosition = new Vector2(
                value01 * sliderBounds.rect.width + sliderBounds.offsetMin.x,
                minHandle.anchoredPosition.y);
        }

        private void SetMax01(float value01)
        {
            maxHandle.anchoredPosition = new Vector2(
                (value01 - 1f) * sliderBounds.rect.width + sliderBounds.offsetMax.x,
                maxHandle.anchoredPosition.y);
        }

        private float PointToValue01(Vector2 localPoint)
        {
            float width = sliderBounds.rect.width;
            return Mathf.Clamp01((localPoint.x + width * 0.5f) / width);
        }

        private void ForwardToParent<T>(Action<T> invoke) where T : IEventSystemHandler
        {
            for (Transform ancestor = transform.parent; ancestor != null; ancestor = ancestor.parent)
            {
                ancestor.GetComponents(_handlerBuffer);
                for (int i = 0; i < _handlerBuffer.Count; i++)
                {
                    if (_handlerBuffer[i] is T handler)
                    {
                        invoke(handler);
                        return;
                    }
                }
            }
        }
    }
}
