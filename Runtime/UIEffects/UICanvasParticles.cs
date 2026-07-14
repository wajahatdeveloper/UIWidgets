using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Renders the attached <see cref="ParticleSystem"/> as canvas geometry so particles draw,
    /// sort, and mask like any other UI element. Each particle becomes a billboard quad with the
    /// main module's color and size over lifetime applied, and texture sheet animation frames
    /// resolved into UV rects.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("UI/Effects/UIWidgets/Canvas Particles")]
    public class UICanvasParticles : MaskableGraphic
    {
        private const int MaxRenderedParticles = 16000; // stays under the 65k vertex limit per canvas mesh

        [SerializeField]
        [Tooltip("Texture drawn on each particle quad. Falls back to the material's main texture.")]
        private Texture particleTexture;

        [SerializeField]
        [Tooltip("Advance the simulation with unscaled time so UI particles ignore Time.timeScale.")]
        private bool useUnscaledTime = true;

        private ParticleSystem _system;
        private ParticleSystemRenderer _systemRenderer;
        private ParticleSystem.Particle[] _buffer;
        private readonly UIVertex[] _quad = new UIVertex[4];

        public override Texture mainTexture
        {
            get
            {
                if (particleTexture != null)
                {
                    return particleTexture;
                }

                if (material != null && material.mainTexture != null)
                {
                    return material.mainTexture;
                }

                return s_WhiteTexture;
            }
        }

        public Texture ParticleTexture
        {
            get => particleTexture;
            set
            {
                particleTexture = value;
                SetMaterialDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _system = GetComponent<ParticleSystem>();
            _systemRenderer = GetComponent<ParticleSystemRenderer>();

            // The built-in renderer would draw the particles a second time in world space.
            if (_systemRenderer != null)
            {
                _systemRenderer.enabled = false;
            }

            raycastTarget = false;
        }

        private void Update()
        {
            if (_system == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                // Keep the canvas mesh in sync with editor particle playback/scrubbing.
                SetVerticesDirty();
                return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _system.Simulate(dt, false, false, true);
            SetVerticesDirty();
        }

        public void Play()
        {
            if (_system == null)
            {
                return;
            }

            _system.time = 0f;
            _system.Play();
        }

        public void Stop()
        {
            if (_system != null)
            {
                _system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void StopEmitting()
        {
            if (_system != null)
            {
                _system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_system == null)
            {
                _system = GetComponent<ParticleSystem>();
                if (_system == null)
                {
                    return;
                }
            }

            var main = _system.main;
            int capacity = Mathf.Min(main.maxParticles, MaxRenderedParticles);
            if (_buffer == null || _buffer.Length < capacity)
            {
                _buffer = new ParticleSystem.Particle[capacity];
            }

            int count = _system.GetParticles(_buffer, capacity);
            bool localSpace = main.simulationSpace == ParticleSystemSimulationSpace.Local;

            var sheet = _system.textureSheetAnimation;
            bool animated = sheet.enabled && sheet.numTilesX > 0 && sheet.numTilesY > 0;
            int totalFrames = animated ? sheet.numTilesX * sheet.numTilesY : 0;
            var frameSize = animated
                ? new Vector2(1f / sheet.numTilesX, 1f / sheet.numTilesY)
                : Vector2.one;

            for (int i = 0; i < count; i++)
            {
                ref var particle = ref _buffer[i];

                Vector2 center = localSpace
                    ? (Vector2)particle.position
                    : (Vector2)transform.InverseTransformPoint(particle.position);

                Color32 color = particle.GetCurrentColor(_system);
                float halfSize = particle.GetCurrentSize(_system) * 0.5f;
                if (halfSize <= 0f)
                {
                    continue;
                }

                var uvRect = animated
                    ? GetFrameUvRect(sheet, particle, totalFrames, frameSize)
                    : new Vector4(0f, 0f, 1f, 1f);

                BuildQuad(center, halfSize, -particle.rotation * Mathf.Deg2Rad, color, uvRect);
                vh.AddUIVertexQuad(_quad);
            }
        }

        private void BuildQuad(Vector2 center, float halfSize, float rotationRad, Color32 color, Vector4 uvRect)
        {
            Vector2 right;
            Vector2 up;
            if (Mathf.Approximately(rotationRad, 0f))
            {
                right = new Vector2(halfSize, 0f);
                up = new Vector2(0f, halfSize);
            }
            else
            {
                float cos = Mathf.Cos(rotationRad);
                float sin = Mathf.Sin(rotationRad);
                right = new Vector2(cos, sin) * halfSize;
                up = new Vector2(-sin, cos) * halfSize;
            }

            _quad[0] = UIVertex.simpleVert;
            _quad[0].color = color;
            _quad[0].position = center - right - up;
            _quad[0].uv0 = new Vector2(uvRect.x, uvRect.y);

            _quad[1] = UIVertex.simpleVert;
            _quad[1].color = color;
            _quad[1].position = center - right + up;
            _quad[1].uv0 = new Vector2(uvRect.x, uvRect.w);

            _quad[2] = UIVertex.simpleVert;
            _quad[2].color = color;
            _quad[2].position = center + right + up;
            _quad[2].uv0 = new Vector2(uvRect.z, uvRect.w);

            _quad[3] = UIVertex.simpleVert;
            _quad[3].color = color;
            _quad[3].position = center + right - up;
            _quad[3].uv0 = new Vector2(uvRect.z, uvRect.y);
        }

        private static Vector4 GetFrameUvRect(ParticleSystem.TextureSheetAnimationModule sheet,
            in ParticleSystem.Particle particle, int totalFrames, Vector2 frameSize)
        {
            float lifeProgress = particle.startLifetime > 0f
                ? Mathf.Clamp01(1f - particle.remainingLifetime / particle.startLifetime)
                : 0f;

            float frameT = Mathf.Clamp01(sheet.frameOverTime.Evaluate(lifeProgress));
            frameT = Mathf.Repeat(frameT * sheet.cycleCount, 1f);

            int frame;
            if (sheet.animation == ParticleSystemAnimationType.SingleRow)
            {
                frame = Mathf.FloorToInt(frameT * sheet.numTilesX);
                int row = sheet.rowMode == ParticleSystemAnimationRowMode.Random
                    ? Mathf.Abs((int)particle.randomSeed % sheet.numTilesY)
                    : sheet.rowIndex;
                frame += row * sheet.numTilesX;
            }
            else
            {
                frame = Mathf.FloorToInt(frameT * totalFrames);
            }

            frame = Mathf.Clamp(frame % totalFrames, 0, totalFrames - 1);

            float uMin = (frame % sheet.numTilesX) * frameSize.x;
            float vMin = 1f - ((frame / sheet.numTilesX) + 1) * frameSize.y;
            return new Vector4(uMin, vMin, uMin + frameSize.x, vMin + frameSize.y);
        }
    }
}
