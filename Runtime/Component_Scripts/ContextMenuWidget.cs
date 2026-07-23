using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform;
using AetherNexus.FoundationPlatform.DebugX;
using AetherNexus.FoundationPlatform.AetherInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AetherNexus.UIWidgets
{
    public enum ContextMenuPlacementMode
    {
        ScreenPosition = 0,
        TargetRect = 1
    }

    public enum ContextMenuPreferredDirection
    {
        Auto = 0,
        DownRight = 1,
        DownLeft = 2,
        UpRight = 3,
        UpLeft = 4
    }

    public sealed class ContextMenuActionData
    {
        public readonly string Id;
        public readonly string Label;
        public readonly bool IsEnabled;
        public readonly bool IsVisible;
        public readonly Action Invoke;

        public ContextMenuActionData(string id, string label, bool isEnabled, bool isVisible, Action invoke)
        {
            Id = id;
            Label = label;
            IsEnabled = isEnabled;
            IsVisible = isVisible;
            Invoke = invoke;
        }
    }

    public sealed class ContextMenuPlacementOptions
    {
        public readonly ContextMenuPlacementMode PlacementMode;
        public readonly Vector2 ScreenPosition;
        public readonly RectTransform TargetRect;
        public readonly ContextMenuPreferredDirection PreferredDirection;
        public readonly Vector2 Offset;
        public readonly bool ClampToViewport;

        public ContextMenuPlacementOptions(
            ContextMenuPlacementMode placementMode,
            Vector2 screenPosition,
            RectTransform targetRect,
            ContextMenuPreferredDirection preferredDirection,
            Vector2 offset,
            bool clampToViewport)
        {
            PlacementMode = placementMode;
            ScreenPosition = screenPosition;
            TargetRect = targetRect;
            PreferredDirection = preferredDirection;
            Offset = offset;
            ClampToViewport = clampToViewport;
        }
    }

    public sealed class ContextMenuRequest
    {
        public readonly IReadOnlyList<ContextMenuActionData> Actions;
        public readonly ContextMenuPlacementOptions Placement;
        public readonly bool CloseOnOutsideClick;
        public readonly bool CloseOnEscape;
        public readonly bool HideOnAction;
        public readonly object CallerContext;

        public ContextMenuRequest(
            IReadOnlyList<ContextMenuActionData> actions,
            ContextMenuPlacementOptions placement,
            bool closeOnOutsideClick,
            bool closeOnEscape,
            bool hideOnAction,
            object callerContext)
        {
            Actions = actions;
            Placement = placement;
            CloseOnOutsideClick = closeOnOutsideClick;
            CloseOnEscape = closeOnEscape;
            HideOnAction = hideOnAction;
            CallerContext = callerContext;
        }
    }

    [AddComponentMenu("UI (Canvas)/Context Menu Widget")]
    public sealed class ContextMenuWidget : SingletonBehaviour<ContextMenuWidget>
    {
        [Header("References")]
        [SerializeField, Required] private RectTransform menuRoot;
        [SerializeField, Required] private RectTransform itemsContainer;
        [SerializeField, Required, ValidateInput(nameof(IsMenuItemPrefabValid), "Menu item prefab must have ContextMenuItemView with ButtonX assigned.")]
        private GameObject menuItemPrefab;
        [SerializeField, Required, ValidateInput(nameof(IsCanvasRootRectTransform), "Root canvas transform must be a RectTransform.")]
        private Canvas rootCanvas;

        private readonly List<ContextMenuItemView> _spawnedItems = new();
        private readonly Vector3[] _targetCorners = new Vector3[4];
        private ContextMenuRequest _activeRequest;
        private bool _isShown;

        public static void Show(ContextMenuRequest request)
        {
            if (request == null)
            {
                DebugX.Builder(LogChannels.UI).Error("[ContextMenuWidget] Show request is null.");
                return;
            }

            ContextMenuWidget widget = Instance;
            if (widget == null)
            {
                DebugX.Builder(LogChannels.UI).Error("[ContextMenuWidget] No ContextMenuWidget instance found in scene.");
                return;
            }

            widget.ShowInternal(request);
        }

        public static void HideAll()
        {
            ContextMenuWidget widget = Instance;
            if (widget == null)
            {
                DebugX.Builder(LogChannels.UI).Error("[ContextMenuWidget] HideAll called but no instance found.");
                return;
            }

            widget.Hide();
        }

        protected override void Awake()
        {
            base.Awake();
            menuRoot.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isShown || _activeRequest == null)
            {
                return;
            }

            if (_activeRequest.CloseOnEscape && UIInput.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                return;
            }

            if (_activeRequest.CloseOnOutsideClick && UIInput.GetMouseButtonDown(0))
            {
                Vector2 pointer = UIInput.MousePosition;
                Camera eventCamera = GetEventCamera();
                bool inside = RectTransformUtility.RectangleContainsScreenPoint(menuRoot, pointer, eventCamera);
                if (!inside)
                {
                    Hide();
                }
            }
        }

        public void Hide()
        {
            ClearSpawnedItems();
            _activeRequest = null;
            _isShown = false;
            menuRoot.gameObject.SetActive(false);
        }

        private void ShowInternal(ContextMenuRequest request)
        {
            if (!ValidateSetup(request))
            {
                return;
            }

            _activeRequest = request;
            ClearSpawnedItems();
            BuildItems(request.Actions, request.HideOnAction);
            if (_spawnedItems.Count == 0)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Warning("[ContextMenuWidget] No visible actions were provided. Hiding menu.");
                Hide();
                return;
            }

            ApplyPlacement(request.Placement);
            menuRoot.gameObject.SetActive(true);
            _isShown = true;
            FocusFirstInteractableItem();
        }

        private bool ValidateSetup(ContextMenuRequest request)
        {
            if (request.Actions == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] Request actions list is null.");
                return false;
            }
            for (int i = 0; i < request.Actions.Count; i++)
            {
                ContextMenuActionData action = request.Actions[i];
                if (action == null)
                {
                    DebugX.Builder(LogChannels.UI).WithContext(this).Error(
                        "[ContextMenuWidget] Request action at index {Index} is null.",
                        i);
                    return false;
                }
                if (action.Invoke == null)
                {
                    DebugX.Builder(LogChannels.UI).WithContext(this).Error(
                        "[ContextMenuWidget] Request action '{ActionId}' is missing invoke callback.",
                        action.Id ?? i.ToString());
                    return false;
                }
            }
            if (request.Placement == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] Request placement options are null.");
                return false;
            }
            if (request.Placement.PlacementMode == ContextMenuPlacementMode.TargetRect && request.Placement.TargetRect == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] Placement mode is TargetRect but target rect is null.");
                return false;
            }
            return true;
        }

        private void BuildItems(IReadOnlyList<ContextMenuActionData> actions, bool hideOnAction)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                ContextMenuActionData action = actions[i];
                if (!action.IsVisible)
                {
                    continue;
                }

                GameObject instance = Instantiate(menuItemPrefab, itemsContainer, false);
                ContextMenuItemView itemView = instance.GetComponent<ContextMenuItemView>();
                if (itemView == null)
                {
                    DebugX.Builder(LogChannels.UI).WithContext(this).Error(
                        "[ContextMenuWidget] Menu item prefab is missing a ContextMenuItemView component.");
                    Destroy(instance);
                    continue;
                }

                _spawnedItems.Add(itemView);
                BindItem(itemView, action, hideOnAction);
                instance.SetActive(true);
            }
        }

        private void BindItem(ContextMenuItemView itemView, ContextMenuActionData action, bool hideOnAction)
        {
            itemView.Bind(action.Label, action.IsEnabled, () => InvokeAction(action, hideOnAction));
        }

        private void InvokeAction(ContextMenuActionData action, bool hideOnAction)
        {
            if (action.IsEnabled)
            {
                action.Invoke.Invoke();
            }

            if (hideOnAction)
            {
                Hide();
            }
        }

        private void ApplyPlacement(ContextMenuPlacementOptions options)
        {
            Transform parentTransform = menuRoot.parent;
            if (parentTransform == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] menuRoot has no parent.");
                return;
            }

            if (!(parentTransform is RectTransform placementParent))
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] menuRoot parent must be a RectTransform.");
                return;
            }

            Vector2 anchorScreenPoint = ResolveAnchorScreenPoint(options);
            Vector2 pivot = ResolvePivot(options.PreferredDirection, anchorScreenPoint);
            menuRoot.pivot = pivot;

            Camera eventCamera = GetEventCamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(placementParent, anchorScreenPoint, eventCamera, out var localPoint))
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuWidget] Failed to convert screen point to menuRoot parent local point.");
                return;
            }

            menuRoot.anchoredPosition = localPoint;
            Canvas.ForceUpdateCanvases();

            if (options.ClampToViewport)
            {
                ClampMenuToParent(placementParent);
            }
        }

        private Vector2 ResolveAnchorScreenPoint(ContextMenuPlacementOptions options)
        {
            Vector2 anchor = options.ScreenPosition;

            if (options.PlacementMode == ContextMenuPlacementMode.TargetRect)
            {
                options.TargetRect.GetWorldCorners(_targetCorners);
                switch (options.PreferredDirection)
                {
                    case ContextMenuPreferredDirection.UpRight:
                        anchor = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), _targetCorners[2]);
                        break;
                    case ContextMenuPreferredDirection.UpLeft:
                        anchor = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), _targetCorners[1]);
                        break;
                    case ContextMenuPreferredDirection.DownLeft:
                        anchor = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), _targetCorners[0]);
                        break;
                    case ContextMenuPreferredDirection.DownRight:
                        anchor = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), _targetCorners[3]);
                        break;
                    default:
                        Vector3 center = (_targetCorners[0] + _targetCorners[2]) * 0.5f;
                        anchor = RectTransformUtility.WorldToScreenPoint(GetEventCamera(), center);
                        break;
                }
            }

            anchor += options.Offset;
            return anchor;
        }

        private Vector2 ResolvePivot(ContextMenuPreferredDirection preferredDirection, Vector2 screenPoint)
        {
            switch (preferredDirection)
            {
                case ContextMenuPreferredDirection.UpRight:
                    return new Vector2(0f, 0f);
                case ContextMenuPreferredDirection.UpLeft:
                    return new Vector2(1f, 0f);
                case ContextMenuPreferredDirection.DownRight:
                    return new Vector2(0f, 1f);
                case ContextMenuPreferredDirection.DownLeft:
                    return new Vector2(1f, 1f);
            }

            float x = screenPoint.x <= (Screen.width * 0.5f) ? 0f : 1f;
            float y = screenPoint.y <= (Screen.height * 0.5f) ? 0f : 1f;
            return new Vector2(x, y);
        }

        private void ClampMenuToParent(RectTransform parentRect)
        {
            Vector2 size = menuRoot.rect.size;
            Vector2 pivot = menuRoot.pivot;
            Vector2 pos = menuRoot.anchoredPosition;
            Rect bounds = parentRect.rect;

            float left = pos.x - size.x * pivot.x;
            float right = left + size.x;
            float bottom = pos.y - size.y * pivot.y;
            float top = bottom + size.y;

            if (left < bounds.xMin)
            {
                pos.x += bounds.xMin - left;
            }
            if (right > bounds.xMax)
            {
                pos.x -= right - bounds.xMax;
            }
            if (bottom < bounds.yMin)
            {
                pos.y += bounds.yMin - bottom;
            }
            if (top > bounds.yMax)
            {
                pos.y -= top - bounds.yMax;
            }

            menuRoot.anchoredPosition = pos;
        }

        private void FocusFirstInteractableItem()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                ContextMenuItemView itemView = _spawnedItems[i];
                if (!itemView.gameObject.activeInHierarchy)
                {
                    continue;
                }

                ButtonX buttonX = itemView.Button;
                if (buttonX.Interactable)
                {
                    buttonX.Select();
                    return;
                }
            }
        }

        private void ClearSpawnedItems()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                ContextMenuItemView itemView = _spawnedItems[i];
                itemView.ClearBinding();
                Destroy(itemView.gameObject);
            }

            _spawnedItems.Clear();
        }

        private Camera GetEventCamera()
        {
            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return rootCanvas.worldCamera;
        }

        private bool IsMenuItemPrefabValid(GameObject prefab)
        {
            if (prefab == null)
            {
                return true;
            }

            if (!prefab.TryGetComponent<ContextMenuItemView>(out var itemView))
            {
                return false;
            }

            return itemView.Button != null;
        }

        private bool IsCanvasRootRectTransform(Canvas canvas)
        {
            return canvas == null || canvas.transform is RectTransform;
        }
    }
}
