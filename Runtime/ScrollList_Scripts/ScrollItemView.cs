using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// A scrollable list item component that handles display and interaction for list items.
    /// Supports automatic component finding and provides flexible configuration options.
    /// </summary>
    [AddComponentMenu("UI/ScrollItemView")]
    public class ScrollItemView : MonoBehaviour, IListItemBinder
    {
        /// <summary>
        /// Binds stock <see cref="ScrollListItemData"/> / <see cref="string"/> payloads used by the
        /// packaged item prefab. Typed rows should use <see cref="ScrollItemView{T}"/> instead.
        /// </summary>
        void IListItemBinder.BindRaw(object data, int index)
        {
            EnsureReferences();
            switch (data)
            {
                case ScrollListItemData item:
                    Configure(item.Title, item.Subtitle, item.Icon);
                    break;
                case string title:
                    Configure(title);
                    break;
                case null:
                    Configure(string.Empty);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"[ScrollList] Prefab '{name}' uses ScrollItemView but data type '{data.GetType().Name}' " +
                        $"is not ScrollListItemData or string. Subclass ScrollItemView<T> and implement Bind, " +
                        $"or pass ScrollListItemData.");
            }
        }

        void IListItemBinder.Unbind()
        {
            ClearButtonListeners();
            Title = string.Empty;
            Subtitle = string.Empty;
            Image = null;
        }

        #region Public Fields
        [Header("Default References")]
        public TextMeshProUGUI title;
        public TextMeshProUGUI subtitle;
        public Image image;
        public ButtonX button;
        [InspectorName("Extra Refs (Assets + Scene)")]
        public List<GameObject> refs;
        #endregion

        #region Events
        /// <summary>
        /// Fired when this item is clicked
        /// </summary>
        public event Action<ScrollItemView> OnClicked;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the title text
        /// </summary>
        public string Title
        {
            get => title?.text ?? string.Empty;
            set
            {
                if (title != null)
                    title.text = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the subtitle text
        /// </summary>
        public string Subtitle
        {
            get => subtitle?.text ?? string.Empty;
            set
            {
                if (subtitle != null)
                    subtitle.text = value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the image sprite
        /// </summary>
        public Sprite Image
        {
            get => image?.sprite;
            set
            {
                if (image != null)
                {
                    image.sprite = value;
                    image.gameObject.SetActive(value != null);
                }
            }
        }

        /// <summary>
        /// Gets whether this item is currently visible
        /// </summary>
        public bool IsVisible => gameObject.activeInHierarchy;
        #endregion

        #region Public Methods
        /// <summary>
        /// Configure this item with common UI content and optional click handler.
        /// Any existing button listeners are cleared before applying the new one.
        /// </summary>
        /// <param name="newTitle">The title text to display</param>
        /// <param name="newSubtitle">The subtitle text to display</param>
        /// <param name="newSprite">The sprite to display in the image component</param>
        /// <param name="onClick">Optional click handler for this item</param>
        public virtual void Configure(string newTitle,
            string newSubtitle = "",
            Sprite newSprite = null,
            Action<ScrollItemView> onClick = null)
        {
            EnsureReferences();

            Title = newTitle;
            Subtitle = newSubtitle;
            Image = newSprite;

            if (button != null)
            {
                button.OnClicked.RemoveAllListeners();
                if (onClick != null)
                {
                    button.OnClicked.AddListener(() => onClick(this));
                }
                button.OnClicked.AddListener(() => OnClicked?.Invoke(this));
            }
        }

        /// <summary>
        /// Update the item's visual state based on visibility
        /// </summary>
        /// <param name="visible">Whether the item should be visible</param>
        public virtual void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Enable or disable the item's interactivity
        /// </summary>
        /// <param name="interactable">Whether the item should be interactable</param>
        public virtual void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.Interactable = interactable;
            }
        }

        /// <summary>
        /// Set the item's color theme
        /// </summary>
        /// <param name="color">The color to apply to the background image</param>
        public virtual void SetColor(Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        /// <summary>
        /// Remove all listeners from this item's button (if present).
        /// </summary>
        public void ClearButtonListeners()
        {
            if (button != null)
            {
                button.OnClicked.RemoveAllListeners();
            }
            OnClicked = null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Ensure references are assigned. Attempts to auto-find if missing.
        /// </summary>
        protected void EnsureReferences()
        {
            if (button == null)
            {
                button = GetComponent<ButtonX>();
            }
            if (image == null)
            {
                image = GetComponentInChildren<Image>(true);
            }
            if (title == null || subtitle == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts != null && texts.Length > 0)
                {
                    if (title == null)
                    {
                        title = texts[0];
                    }
                    if (subtitle == null && texts.Length > 1)
                    {
                        subtitle = texts[1];
                    }
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
        }

        /// <summary>
        /// Per-frame update hook for custom logic.
        /// The base class intentionally does NOT declare Unity's Update() to avoid
        /// paying the per-MonoBehaviour managed Update cost on inert rows in large/pooled
        /// lists. Derived classes that need per-frame behavior should declare their own
        /// Update() and call DoUpdate() (or implement logic directly).
        /// </summary>
        public virtual void DoUpdate()
        {
            // Override in derived classes for custom update logic
        }
        #endregion
    }
}