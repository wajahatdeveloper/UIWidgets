using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
    /// <summary>
    /// Fades the attached <see cref="Graphic"/> through the alpha channel of a mask texture,
    /// giving soft-edged masking without stencil hard cuts. The mask texture is stretched over
    /// <see cref="MaskArea"/> (this graphic's rect by default) and sampled per pixel by the
    /// UIWidgets/SoftMask shader.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/UIWidgets/Soft Mask")]
    public class UISoftMask : MonoBehaviour
    {
        private const string ShaderName = "UIWidgets/SoftMask";

        private static readonly int SoftMaskTexId = Shader.PropertyToID("_SoftMaskTex");
        private static readonly int CutoffId = Shader.PropertyToID("_SoftMaskCutoff");
        private static readonly int InvertId = Shader.PropertyToID("_SoftMaskInvert");
        private static readonly int WorldToMaskId = Shader.PropertyToID("_WorldToMask");
        private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");

        [SerializeField]
        [Tooltip("Texture whose alpha channel drives visibility. Opaque = visible, transparent = hidden.")]
        private Texture maskTexture;

        [SerializeField]
        [Tooltip("Rect the mask texture is stretched over. Leave empty to use this graphic's own rect.")]
        private RectTransform maskArea;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Mask alpha at or below this value becomes fully transparent; the remainder stays soft.")]
        private float cutoff;

        [SerializeField]
        [Tooltip("Use 1 - alpha, so opaque areas of the mask hide the graphic instead.")]
        private bool invert;

        private Graphic _graphic;
        private Material _material;
        private bool _warnedMissingShader;

        public Texture MaskTexture
        {
            get => maskTexture;
            set => maskTexture = value;
        }

        public RectTransform MaskArea
        {
            get => maskArea;
            set => maskArea = value;
        }

        public float Cutoff
        {
            get => cutoff;
            set => cutoff = Mathf.Clamp01(value);
        }

        public bool Invert
        {
            get => invert;
            set => invert = value;
        }

        private void OnEnable()
        {
            _graphic = GetComponent<Graphic>();

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                if (!_warnedMissingShader)
                {
                    _warnedMissingShader = true;
                    DebugX.Logger(LogChannels.UI).Error(
                        "[UI] UISoftMask on '{Name}' could not find shader '{Shader}'. Is UISoftMask.shader in the project?",
                        name, ShaderName);
                }

                enabled = false;
                return;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _graphic.material = _material;
        }

        private void OnDisable()
        {
            if (_graphic != null && _graphic.material == _material)
            {
                _graphic.material = null;
            }

            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }

                _material = null;
            }
        }

        private void LateUpdate()
        {
            if (_material == null)
            {
                return;
            }

            var area = maskArea != null ? maskArea : (RectTransform)transform;
            var rect = area.rect;

            _material.SetTexture(SoftMaskTexId, maskTexture != null ? maskTexture : Texture2D.whiteTexture);
            _material.SetFloat(CutoffId, cutoff);
            _material.SetFloat(InvertId, invert ? 1f : 0f);
            _material.SetMatrix(WorldToMaskId, area.worldToLocalMatrix);
            _material.SetVector(MaskRectId, new Vector4(
                rect.xMin,
                rect.yMin,
                rect.width > 0f ? 1f / rect.width : 0f,
                rect.height > 0f ? 1f / rect.height : 0f));
        }
    }
}
