using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Per-pixel raycast filtering for an <see cref="Image"/>: pointer events only register where
    /// the sprite's alpha exceeds <see cref="AlphaThreshold"/>. Supports Simple and Sliced image
    /// types. The sprite texture must have Read/Write enabled in its import settings.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/UIWidgets/Raycast Alpha Mask")]
    public class UIRaycastAlphaMask : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField, Range(0f, 1f)]
        [Tooltip("Pixels with alpha at or below this value let the raycast pass through.")]
        private float alphaThreshold;

        private Image _image;
        private bool _warnedUnreadable;

        public float AlphaThreshold
        {
            get => alphaThreshold;
            set => alphaThreshold = Mathf.Clamp01(value);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            var sprite = _image.sprite;
            if (sprite == null)
            {
                return true;
            }

            var texture = sprite.texture;
            if (texture == null || !texture.isReadable)
            {
                if (!_warnedUnreadable)
                {
                    _warnedUnreadable = true;
                    DebugX.Logger(LogChannels.UI).Error(
                        "[UI] UIRaycastAlphaMask on '{Name}' needs a readable sprite texture. Enable Read/Write in the texture import settings. Falling back to rect raycasts.",
                        name);
                }

                return true;
            }

            var rectTransform = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
            {
                return false;
            }

            var rect = rectTransform.rect;

            // Shift into bottom-left-origin rect coordinates.
            local.x += rectTransform.pivot.x * rect.width;
            local.y += rectTransform.pivot.y * rect.height;

            if (local.x < 0f || local.x > rect.width || local.y < 0f || local.y > rect.height)
            {
                return false;
            }

            var textureRect = sprite.textureRect;
            var border = sprite.border;
            bool sliced = _image.type == Image.Type.Sliced;

            int texelX = MapToTexel(local.x, rect.width, textureRect.x, textureRect.width, border.x, border.z, sliced);
            int texelY = MapToTexel(local.y, rect.height, textureRect.y, textureRect.height, border.y, border.w, sliced);

            return texture.GetPixel(texelX, texelY).a > alphaThreshold;
        }

        /// <summary>
        /// Maps a rect-local coordinate on one axis to a texel coordinate, accounting for 9-slice
        /// borders when the image is sliced: border regions map 1:1, the middle region stretches.
        /// </summary>
        private static int MapToTexel(float localPos, float rectSize, float texMin, float texSize,
            float borderMin, float borderMax, bool sliced)
        {
            if (!sliced || (borderMin <= 0f && borderMax <= 0f))
            {
                return Mathf.FloorToInt(texMin + texSize * (localPos / rectSize));
            }

            if (localPos < borderMin)
            {
                return Mathf.FloorToInt(texMin + localPos);
            }

            if (localPos > rectSize - borderMax)
            {
                return Mathf.FloorToInt(texMin + texSize - (rectSize - localPos));
            }

            float stretch = (localPos - borderMin) / (rectSize - borderMin - borderMax);
            return Mathf.FloorToInt(texMin + borderMin + stretch * (texSize - borderMin - borderMax));
        }
    }
}
