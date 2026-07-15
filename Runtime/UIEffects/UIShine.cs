using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Draws an animated highlight band that sweeps across the rect — the classic "shine" pass
    /// over a button or card. The band is generated as canvas geometry tinted by
    /// <see cref="Graphic.color"/>, so parenting under a <see cref="Mask"/> (or RectMask2D)
    /// clips it to the underlying shape. Autoplays on enable by default, or call
    /// <see cref="Play"/> to trigger a single sweep.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI (Canvas)/Effects/Shine")]
    public class UIShine : MaskableGraphic
    {
        [SerializeField, Range(-1f, 1f)]
        [Tooltip("Sweep position: -1 fully below the rect, +1 fully above. Animated during playback.")]
        private float offset = -1f;

        [SerializeField, Range(0.05f, 1f)]
        [Tooltip("Band thickness as a fraction of the rect height.")]
        private float width = 0.25f;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("How much of the band fades out towards its edges. 0 = hard band, 1 = fully soft.")]
        private float smoothness = 0.6f;

        [SerializeField, Range(-45f, 45f)]
        [Tooltip("Tilt of the band in degrees.")]
        private float angle = -8f;

        [Header("Playback")]
        [SerializeField]
        private bool autoPlay = true;

        [SerializeField]
        private bool loop = true;

        [SerializeField, Min(0.05f)]
        private float sweepDuration = 1f;

        [SerializeField, Min(0f)]
        [Tooltip("Pause between sweeps when looping.")]
        private float loopDelay = 1f;

        private bool _playing;
        private float _elapsed;

        public float Offset
        {
            get => offset;
            set => SetOffset(Mathf.Clamp(value, -1f, 1f));
        }

        public float Width
        {
            get => width;
            set
            {
                width = Mathf.Clamp(value, 0.05f, 1f);
                SetVerticesDirty();
            }
        }

        public float Smoothness
        {
            get => smoothness;
            set
            {
                smoothness = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        public bool IsPlaying => _playing;

        /// <summary>Starts a sweep from the bottom edge. Restarts if one is already running.</summary>
        public void Play()
        {
            _playing = true;
            _elapsed = 0f;
            SetOffset(-1f);
        }

        /// <summary>Stops playback and hides the band.</summary>
        public void Stop()
        {
            _playing = false;
            SetOffset(-1f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (autoPlay && Application.isPlaying)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !_playing)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed <= sweepDuration)
            {
                SetOffset(Mathf.Lerp(-1f, 1f, _elapsed / sweepDuration));
            }
            else if (loop)
            {
                if (_elapsed >= sweepDuration + loopDelay)
                {
                    _elapsed = 0f;
                    SetOffset(-1f);
                }
                else
                {
                    SetOffset(1f); // parked off-rect while waiting for the next sweep
                }
            }
            else
            {
                _playing = false;
                SetOffset(-1f);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (offset <= -1f || offset >= 1f)
            {
                return;
            }

            var rect = GetPixelAdjustedRect();
            float bandHalf = Mathf.Max(rect.height * width * 0.5f, 0.001f);
            float coreHalf = bandHalf * (1f - smoothness);
            float centerY = Mathf.Lerp(rect.yMin - bandHalf, rect.yMax + bandHalf, offset * 0.5f + 0.5f);

            // Extend past the rect horizontally so the tilted band still covers the full width.
            float pad = (rect.width + rect.height) * 0.5f;
            float left = rect.xMin - pad;
            float right = rect.xMax + pad;

            Color32 solid = color;
            Color32 faded = color;
            faded.a = 0;

            float radians = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            Vector2 pivot = rect.center;

            // Four rows of two vertices: transparent edge, solid core, solid core, transparent edge.
            AddRow(vh, left, right, centerY - bandHalf, faded, 0f, pivot, cos, sin);
            AddRow(vh, left, right, centerY - coreHalf, solid, 0.5f - 0.5f * (1f - smoothness), pivot, cos, sin);
            AddRow(vh, left, right, centerY + coreHalf, solid, 0.5f + 0.5f * (1f - smoothness), pivot, cos, sin);
            AddRow(vh, left, right, centerY + bandHalf, faded, 1f, pivot, cos, sin);

            for (int row = 0; row < 3; row++)
            {
                int i = row * 2;
                vh.AddTriangle(i, i + 2, i + 3);
                vh.AddTriangle(i + 3, i + 1, i);
            }
        }

        private static void AddRow(VertexHelper vh, float left, float right, float y, Color32 rowColor,
            float v, Vector2 pivot, float cos, float sin)
        {
            vh.AddVert(RotateAround(new Vector2(left, y), pivot, cos, sin), rowColor, new Vector2(0f, v));
            vh.AddVert(RotateAround(new Vector2(right, y), pivot, cos, sin), rowColor, new Vector2(1f, v));
        }

        private static Vector3 RotateAround(Vector2 point, Vector2 pivot, float cos, float sin)
        {
            Vector2 delta = point - pivot;
            return new Vector3(
                pivot.x + delta.x * cos - delta.y * sin,
                pivot.y + delta.x * sin + delta.y * cos,
                0f);
        }

        private void SetOffset(float value)
        {
            if (Mathf.Approximately(offset, value))
            {
                return;
            }

            offset = value;
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            raycastTarget = false;
            color = new Color(1f, 1f, 1f, 0.5f);
        }
#endif
    }
}
