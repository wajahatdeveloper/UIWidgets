using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
    /// <summary>
    /// Filters UI raycasts based on the alpha value of the underlying <see cref="Image"/> sprite.
    /// Supports packed sprites by sampling within <see cref="Sprite.textureRect"/> and exposes a configurable threshold.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class AlphaButtonClickMask : MonoBehaviour, ICanvasRaycastFilter 
    {
        [Tooltip("Pixels with alpha below this value are treated as not clickable (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _alphaThreshold = 0.1f;

        [Tooltip("If enabled, click-through happens where alpha is ABOVE the threshold instead of below.")]
        [SerializeField] private bool _invert = false;

        [Tooltip("Sample inside Sprite.textureRect (recommended for atlased/trimmed sprites). If disabled, uses Sprite.rect.")]
        [SerializeField] private bool _useTextureRect = true;

        [Header("Image Type Handling")]
        [Tooltip("Use alpha masking for Sliced images by respecting 9-slice borders.")]
        [SerializeField] private bool _supportSliced = true;

        [Tooltip("Use alpha masking for Filled images by respecting fill amount and method.")]
        [SerializeField] private bool _supportFilled = true;

        [Tooltip("Use alpha masking for Tiled images (center region tiles). Border regions behave like Sliced.")]
        [SerializeField] private bool _supportTiled = false;

        protected Image _image;
        Texture2D _texture;
        Sprite _sprite;

        void Awake()
        {
            _image = GetComponent<Image>();
        }

        void OnEnable()
        {
            CacheSpriteAndTexture();
            ValidateTextureReadable();
        }

        void OnValidate()
        {
            // Keep values in valid ranges when edited in the Inspector
            _alphaThreshold = Mathf.Clamp01(_alphaThreshold);
        }

        void CacheSpriteAndTexture()
        {
            _sprite = _image != null ? _image.sprite : null;
            _texture = _sprite != null ? _sprite.texture : null;
        }

        void ValidateTextureReadable()
        {
		if (_texture == null)
		{
			DebugX.Builder(LogChannels.UI).WithContext(this).Error("AlphaButtonClickMask requires an Image with a readable Texture2D.");
			return;
		}

		// Cheaper readability check: avoids allocating a full Color32[] just to detect a thrown exception
		if (!_texture.isReadable)
		{
			DebugX.Builder(LogChannels.UI).WithContext(this).Error("AlphaButtonClickMask requires a readable Texture2D (enable Read/Write in import settings).");
		}
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // Let Image's own logic reject first (handles Filled geometry and various layout edge cases)
            if (_supportFilled && _image != null)
            {
                if (!_image.IsRaycastLocationValid(sp, eventCamera))
                    return false;
            }

            if (_image == null)
                _image = GetComponent<Image>();

            if (_image == null)
                return true; // No image to validate against

            if (_image.sprite == null || _image.sprite.texture == null)
                return true; // Nothing to test -> let it pass

            // Re-cache if sprite changed at runtime
            if (_sprite != _image.sprite)
            {
                CacheSpriteAndTexture();
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_image.rectTransform, sp, eventCamera, out localPoint))
                return true;

            // Compute UV for the point according to Image.type and settings
            if (!TryGetSpriteUV(localPoint, out var u, out var v))
                return false;

            var c = _image.sprite.texture.GetPixelBilinear(u, v);

            var isOpaque = c.a >= _alphaThreshold;
            return _invert ? !isOpaque : isOpaque;
        }

        bool TryGetSpriteUV(in Vector2 localPoint, out float u, out float v)
        {
            u = v = 0f;

            var rectTransform = _image.rectTransform;
            var rect = rectTransform.rect;
            var pivot = rectTransform.pivot;

            // Normalize local position into [0,1] within the displayed content rect
            Vector2 normalized;

            var imageType = _image.type;

            if ((imageType == Image.Type.Simple || imageType == Image.Type.Filled))
            {
                // Handle preserveAspect by calculating the active sub-rect used for drawing
                if (_image.preserveAspect)
                {
                    var spriteRect = _image.sprite.rect;
                    var spriteAspect = spriteRect.width / spriteRect.height;

                    float usedWidth = rect.width;
                    float usedHeight = rect.height;
                    if (rect.width / rect.height > spriteAspect)
                    {
                        usedWidth = rect.height * spriteAspect;
                    }
                    else
                    {
                        usedHeight = rect.width / spriteAspect;
                    }

                    var offsetX = (rect.width - usedWidth) * (pivot.x - 0.5f);
                    var offsetY = (rect.height - usedHeight) * (pivot.y - 0.5f);

                    var adjusted = new Vector2(localPoint.x - offsetX, localPoint.y - offsetY);

                    normalized = new Vector2(
                        pivot.x + (adjusted.x / usedWidth),
                        pivot.y + (adjusted.y / usedHeight)
                    );
                }
                else
                {
                    normalized = new Vector2(
                        pivot.x + (localPoint.x / rect.width),
                        pivot.y + (localPoint.y / rect.height)
                    );
                }
            }
            else
            {
                // For Sliced/Tiled we start with full rect normalization
                normalized = new Vector2(
                    pivot.x + (localPoint.x / rect.width),
                    pivot.y + (localPoint.y / rect.height)
                );
            }

            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
                return false;

            var sprite = _image.sprite;
            var texture = sprite.texture;
            var sampleRect = _useTextureRect ? sprite.textureRect : sprite.rect;

            // Map normalized to pixels according to Image type
            float pixelX, pixelY;

            if (imageType == Image.Type.Sliced && _supportSliced && sprite.border.sqrMagnitude > 0.0f)
            {
                // 9-slice mapping using border ratios
                var bL = sprite.border.x;
                var bB = sprite.border.y;
                var bR = sprite.border.z;
                var bT = sprite.border.w;

                var leftNorm = bL / sampleRect.width;
                var rightNorm = 1f - (bR / sampleRect.width);
                var bottomNorm = bB / sampleRect.height;
                var topNorm = 1f - (bT / sampleRect.height);

                // X
                if (normalized.x < leftNorm)
                {
                    var t = normalized.x / Mathf.Max(leftNorm, 1e-6f);
                    pixelX = sampleRect.x + t * bL;
                }
                else if (normalized.x > rightNorm)
                {
                    var t = (normalized.x - rightNorm) / Mathf.Max(1f - rightNorm, 1e-6f);
                    pixelX = sampleRect.x + (sampleRect.width - bR) + t * bR;
                }
                else
                {
                    var t = (normalized.x - leftNorm) / Mathf.Max(rightNorm - leftNorm, 1e-6f);
                    pixelX = sampleRect.x + bL + t * (sampleRect.width - bL - bR);
                }

                // Y
                if (normalized.y < bottomNorm)
                {
                    var t = normalized.y / Mathf.Max(bottomNorm, 1e-6f);
                    pixelY = sampleRect.y + t * bB;
                }
                else if (normalized.y > topNorm)
                {
                    var t = (normalized.y - topNorm) / Mathf.Max(1f - topNorm, 1e-6f);
                    pixelY = sampleRect.y + (sampleRect.height - bT) + t * bT;
                }
                else
                {
                    var t = (normalized.y - bottomNorm) / Mathf.Max(topNorm - bottomNorm, 1e-6f);
                    pixelY = sampleRect.y + bB + t * (sampleRect.height - bB - bT);
                }
            }
            else if (imageType == Image.Type.Tiled && _supportTiled)
            {
                // Tile the center region; borders behave like sliced
                var bL = sprite.border.x;
                var bB = sprite.border.y;
                var bR = sprite.border.z;
                var bT = sprite.border.w;

                var leftNorm = bL / sampleRect.width;
                var rightNorm = 1f - (bR / sampleRect.width);
                var bottomNorm = bB / sampleRect.height;
                var topNorm = 1f - (bT / sampleRect.height);

                // X
                if (normalized.x < leftNorm)
                {
                    var t = normalized.x / Mathf.Max(leftNorm, 1e-6f);
                    pixelX = sampleRect.x + t * bL;
                }
                else if (normalized.x > rightNorm)
                {
                    var t = (normalized.x - rightNorm) / Mathf.Max(1f - rightNorm, 1e-6f);
                    pixelX = sampleRect.x + (sampleRect.width - bR) + t * bR;
                }
                else
                {
                    var centerWidth = Mathf.Max(sampleRect.width - bL - bR, 1e-6f);
                    var t = (normalized.x - leftNorm) / Mathf.Max(rightNorm - leftNorm, 1e-6f);
                    t = Mathf.Repeat(t, 1f);
                    pixelX = sampleRect.x + bL + t * centerWidth;
                }

                // Y
                if (normalized.y < bottomNorm)
                {
                    var t = normalized.y / Mathf.Max(bottomNorm, 1e-6f);
                    pixelY = sampleRect.y + t * bB;
                }
                else if (normalized.y > topNorm)
                {
                    var t = (normalized.y - topNorm) / Mathf.Max(1f - topNorm, 1e-6f);
                    pixelY = sampleRect.y + (sampleRect.height - bT) + t * bT;
                }
                else
                {
                    var centerHeight = Mathf.Max(sampleRect.height - bB - bT, 1e-6f);
                    var t = (normalized.y - bottomNorm) / Mathf.Max(topNorm - bottomNorm, 1e-6f);
                    t = Mathf.Repeat(t, 1f);
                    pixelY = sampleRect.y + bB + t * centerHeight;
                }
            }
            else
            {
                // Simple/Filled linear mapping across the sampled rect
                pixelX = sampleRect.x + normalized.x * sampleRect.width;
                pixelY = sampleRect.y + normalized.y * sampleRect.height;
            }

            u = pixelX / texture.width;
            v = pixelY / texture.height;
            return true;
        }
    }
}
