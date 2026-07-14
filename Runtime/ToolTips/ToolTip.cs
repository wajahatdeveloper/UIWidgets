using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
    [RequireComponent(typeof(RectTransform))]
    public class ToolTip : MonoBehaviour
    {
        //text of the tooltip
#if UNITY_2022_1_OR_NEWER
        private TMPro.TMP_Text _text;
#else
        private Text _text;
#endif
        private RectTransform _rectTransform, canvasRectTransform;

        [Tooltip("The canvas used by the tooltip as positioning and scaling reference. Should usually be the root Canvas of the hierarchy this component is in")]
        public Canvas canvas;

        [Tooltip("Sets if tooltip triggers will run ForceUpdateCanvases and refresh the tooltip's layout group " +
            "(if any) when hovered, in order to prevent momentousness misplacement sometimes caused by ContentSizeFitters")]
        public bool tooltipTriggersCanForceCanvasUpdate = false;

        /// <summary>
        /// the tooltip's Layout Group, if any
        /// </summary>
        private LayoutGroup _layoutGroup;

        //if the tooltip is inside a UI element
        private bool _inside;

        private float width, height;//, canvasWidth, canvasHeight;

        [Tooltip("Pixel offset applied from the chosen anchor position.")]
        public float YShift,xShift;

        [Header("Appearance")]
        [Range(0f, 1f)]
        [Tooltip("Global alpha for the tooltip root CanvasGroup if present.")]
        public float alpha = 1f;

        [Tooltip("If true, disables raycasts on all child Graphics to avoid flicker from pointer exit/enter events.")]
        public bool disableAllGraphicRaycasts = true;

        [HideInInspector]
        public RenderMode guiMode;

        private Camera _guiCamera;

        public Camera GuiCamera
        {
            // Resolved once in Awake from canvas.worldCamera and treated as an invariant.
            // For overlay canvases this is null, which is correct (overlay math doesn't use a camera).
            // Do not fall back to Camera.main here: this getter is read per-frame while hovered.
            get { return _guiCamera; }
        }

        private Vector3 screenLowerLeft, screenUpperRight, shiftingVector;

        /// <summary>
        /// a screen-space point where the tooltip would be placed before applying X and Y shifts and border checks
        /// </summary>
        private Vector3 baseTooltipPos;

        private Vector3 newTTPos;
        private Vector3 adjustedNewTTPos;
        private Vector3 adjustedTTLocalPos;
        private Vector3 shifterForBorders;

        private float borderTest;

        // Cached child Graphics for raycast disabling; resolved once to avoid a per-frame
        // GetComponentsInChildren allocation while the tooltip is hovered.
        private Graphic[] _cachedGraphics;

        // Standard Singleton Access
        private static ToolTip instance;
        
        public static ToolTip Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_2023_1_OR_NEWER
                    instance = FindFirstObjectByType<ToolTip>();
#else
                    instance = FindObjectOfType<ToolTip>();
#endif
                }
                return instance;
            }
        }

        
        void Reset() {
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null) {
                canvas = canvas.rootCanvas;
            }
        }

        // Use this for initialization
        public void Awake()
        {
            instance = this;
            if (!canvas) {
                canvas = GetComponentInParent<Canvas>();
                if (canvas != null) {
                    canvas = canvas.rootCanvas;
                }
            }

            if (canvas == null) {
                DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:ToolTip] No parent Canvas found. ToolTip must be placed under a Canvas. Disabling component.");
                enabled = false;
                return;
            }

            _guiCamera = canvas.worldCamera;
            guiMode = canvas.renderMode;
            _rectTransform = GetComponent<RectTransform>();
            canvasRectTransform = canvas.GetComponent<RectTransform>();
            _layoutGroup = GetComponentInChildren<LayoutGroup>();

#if UNITY_2022_1_OR_NEWER
            _text = GetComponentInChildren<TMPro.TMP_Text>();
#else
            _text = GetComponentInChildren<Text>();
#endif

            _inside = false;

            this.gameObject.SetActive(false);
            ApplyAlphaIfAvailable();
            ApplyRaycastSettings();
        }

        public void SetTooltip(string ttext)
        {
            SetTooltip(ttext, transform.position);
        }

        //Call this function externally to set the text of the template and activate the tooltip
        public void SetTooltip(string ttext, Vector3 basePos, bool refreshCanvasesBeforeGetSize = false)
        {

            baseTooltipPos = basePos;

            //set the text
            if (_text) {
                _text.text = ttext;
            }
            else {
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:ToolTip] No child Text component. Cannot set tooltip text.");
            }

            ContextualTooltipUpdate(refreshCanvasesBeforeGetSize);

        }

        //call this function on mouse exit to deactivate the template
        public void HideTooltip()
        {
            gameObject.SetActive(false);
            _inside = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (_inside)
            {
                ContextualTooltipUpdate();
            }
        }

        /// <summary>
        /// forces rebuilding of Canvases in order to update the tooltip's content size fitting.
        /// Can prevent the tooltip from being visibly misplaced for one frame when being resized.
        /// Only runs if tooltipTriggersCanForceCanvasUpdate is true
        /// </summary>
        public void RefreshTooltipSize() {
            if (tooltipTriggersCanForceCanvasUpdate) {
                Canvas.ForceUpdateCanvases();

                if (_layoutGroup) {
                    _layoutGroup.enabled = false;
                    _layoutGroup.enabled = true;
                }
                
            }
            
        }

        /// <summary>
        /// Runs the appropriate tooltip placement method, according to the parent canvas's render mode
        /// </summary>
        /// <param name="refreshCanvasesBeforeGettingSize"></param>
        public void ContextualTooltipUpdate(bool refreshCanvasesBeforeGettingSize = false) {
            switch (guiMode) {
                case RenderMode.ScreenSpaceCamera:
                    OnScreenSpaceCamera(refreshCanvasesBeforeGettingSize);
                    break;
                case RenderMode.ScreenSpaceOverlay:
                    OnScreenSpaceOverlay(refreshCanvasesBeforeGettingSize);
                    break;
            }
        }

        //main tooltip edge of screen guard and movement - camera
        public void OnScreenSpaceCamera(bool refreshCanvasesBeforeGettingSize = false)
        {
            shiftingVector.x = xShift;
            shiftingVector.y = YShift;

            baseTooltipPos.z = canvas.planeDistance;

            newTTPos = GuiCamera.ScreenToViewportPoint(baseTooltipPos - shiftingVector);
            adjustedNewTTPos = GuiCamera.ViewportToWorldPoint(newTTPos);

            gameObject.SetActive(true);
            ApplyAlphaIfAvailable();
            ApplyRaycastSettings();

            if (refreshCanvasesBeforeGettingSize) RefreshTooltipSize();

            //consider scaled dimensions when comparing against the edges
            width = transform.lossyScale.x * _rectTransform.sizeDelta[0];
            height = transform.lossyScale.y * _rectTransform.sizeDelta[1];

            // check and solve problems for the tooltip that goes out of the screen on the horizontal axis

            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, Vector2.zero, GuiCamera, out screenLowerLeft);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRectTransform, new Vector2(Screen.width, Screen.height), GuiCamera, out screenUpperRight);


            //check for right edge of screen
            borderTest = (adjustedNewTTPos.x + width / 2);
            if (borderTest > screenUpperRight.x)
            {
                shifterForBorders.x = borderTest - screenUpperRight.x;
                adjustedNewTTPos.x -= shifterForBorders.x;
            }
            //check for left edge of screen
            borderTest = (adjustedNewTTPos.x - width / 2);
            if (borderTest < screenLowerLeft.x)
            {
                shifterForBorders.x = screenLowerLeft.x - borderTest;
                adjustedNewTTPos.x += shifterForBorders.x;
            }

            // check and solve problems for the tooltip that goes out of the screen on the vertical axis

            //check for lower edge of the screen
            borderTest = (adjustedNewTTPos.y - height / 2);
            if (borderTest < screenLowerLeft.y) {
                shifterForBorders.y = screenLowerLeft.y - borderTest;
                adjustedNewTTPos.y += shifterForBorders.y;
            }

            //check for upper edge of the screen
            borderTest = (adjustedNewTTPos.y + height / 2);
            if (borderTest > screenUpperRight.y)
            {
                shifterForBorders.y = borderTest - screenUpperRight.y;
                adjustedNewTTPos.y -= shifterForBorders.y;
            }

            //failed attempt to circumvent issues caused when rotating the camera
            adjustedNewTTPos = transform.rotation * adjustedNewTTPos;

            transform.position = adjustedNewTTPos;
            adjustedTTLocalPos = transform.localPosition;
            adjustedTTLocalPos.z = 0;
            transform.localPosition = adjustedTTLocalPos;

            _inside = true;
        }


        //main tooltip edge of screen guard and movement - overlay
        public void OnScreenSpaceOverlay(bool refreshCanvasesBeforeGettingSize = false) {
            shiftingVector.x = xShift;
            shiftingVector.y = YShift;
            newTTPos = (baseTooltipPos - shiftingVector) / canvas.scaleFactor;
            adjustedNewTTPos = newTTPos;

            gameObject.SetActive(true);
            ApplyAlphaIfAvailable();
            ApplyRaycastSettings();

            if (refreshCanvasesBeforeGettingSize) RefreshTooltipSize();

            width = _rectTransform.sizeDelta[0];
            height = _rectTransform.sizeDelta[1];

            // check and solve problems for the tooltip that goes out of the screen on the horizontal axis
            //screen's 0 = overlay canvas's 0 (always?)
            screenLowerLeft = Vector3.zero;
            screenUpperRight = canvasRectTransform.sizeDelta;

            //check for right edge of screen
            borderTest = (newTTPos.x + width / 2);
            if (borderTest > screenUpperRight.x) {
                shifterForBorders.x = borderTest - screenUpperRight.x;
                adjustedNewTTPos.x -= shifterForBorders.x;
            }
            //check for left edge of screen
            borderTest = (adjustedNewTTPos.x - width / 2);
            if (borderTest < screenLowerLeft.x) {
                shifterForBorders.x = screenLowerLeft.x - borderTest;
                adjustedNewTTPos.x += shifterForBorders.x;
            }

            // check and solve problems for the tooltip that goes out of the screen on the vertical axis

            //check for lower edge of the screen
            borderTest = (adjustedNewTTPos.y - height / 2);
            if (borderTest < screenLowerLeft.y) {
                shifterForBorders.y = screenLowerLeft.y - borderTest;
                adjustedNewTTPos.y += shifterForBorders.y;
            }

            //check for upper edge of the screen
            borderTest = (adjustedNewTTPos.y + height / 2);
            if (borderTest > screenUpperRight.y) {
                shifterForBorders.y = borderTest - screenUpperRight.y;
                adjustedNewTTPos.y -= shifterForBorders.y;
            }

            //remove scale factor for the actual positioning of the TT
            adjustedNewTTPos *= canvas.scaleFactor;
            transform.position = adjustedNewTTPos;

            _inside = true;
        }

        private void ApplyAlphaIfAvailable()
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = alpha;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }

        private void ApplyRaycastSettings()
        {
            if (!disableAllGraphicRaycasts) return;
            // Cache once: the child Graphics set is stable for this tooltip, and this method is
            // re-invoked every frame while hovered. Avoids the per-frame array allocation.
            if (_cachedGraphics == null)
            {
                _cachedGraphics = GetComponentsInChildren<Graphic>(true);
            }
            for (int i = 0; i < _cachedGraphics.Length; i++)
            {
                _cachedGraphics[i].raycastTarget = false;
            }
        }
    }
}