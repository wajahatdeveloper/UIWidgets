using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
    /// <summary>
    /// Tints the attached <see cref="Graphic"/>'s vertices along a gradient. The gradient runs
    /// horizontally, vertically, or along an arbitrary angle, sourced from either a two-color
    /// pair or a full <see cref="UnityEngine.Gradient"/>, and blends with the existing vertex
    /// colors by multiply, override, or add.
    ///
    /// Colors are applied per vertex, so multi-stop gradients only show all their stops on
    /// tessellated graphics (text, sliced images); a simple quad interpolates its four corners.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI (Canvas)/Effects/Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        public enum Direction
        {
            Horizontal,
            Vertical,
            Angle,
        }

        public enum ColorSource
        {
            TwoColors,
            Gradient,
        }

        public enum BlendMode
        {
            Multiply,
            Override,
            Additive,
        }

        [SerializeField]
        private Direction direction = Direction.Vertical;

        [SerializeField, Range(0f, 360f)]
        [Tooltip("Gradient direction in degrees, counter-clockwise from +X. Only used when Direction is Angle.")]
        private float angle;

        [SerializeField]
        private ColorSource colorSource = ColorSource.TwoColors;

        [SerializeField]
        [Tooltip("Color at the start of the gradient (left for horizontal, top for vertical).")]
        private Color startColor = Color.white;

        [SerializeField]
        [Tooltip("Color at the end of the gradient (right for horizontal, bottom for vertical).")]
        private Color endColor = Color.black;

        [SerializeField]
        private UnityEngine.Gradient gradient = CreateDefaultGradient();

        [SerializeField]
        private BlendMode blendMode = BlendMode.Multiply;

        public Direction GradientDirection
        {
            get => direction;
            set
            {
                direction = value;
                MarkDirty();
            }
        }

        public float Angle
        {
            get => angle;
            set
            {
                angle = Mathf.Repeat(value, 360f);
                MarkDirty();
            }
        }

        public ColorSource Source
        {
            get => colorSource;
            set
            {
                colorSource = value;
                MarkDirty();
            }
        }

        public Color StartColor
        {
            get => startColor;
            set
            {
                startColor = value;
                MarkDirty();
            }
        }

        public Color EndColor
        {
            get => endColor;
            set
            {
                endColor = value;
                MarkDirty();
            }
        }

        public UnityEngine.Gradient Gradient
        {
            get => gradient;
            set
            {
                gradient = value;
                MarkDirty();
            }
        }

        public BlendMode Blend
        {
            get => blendMode;
            set
            {
                blendMode = value;
                MarkDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            int count = vh.currentVertCount;
            if (!IsActive() || count == 0)
            {
                return;
            }

            Vector2 dir = GetDirectionVector();

            // Project every vertex onto the gradient axis to find its 0..1 position.
            var vertex = default(UIVertex);
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float projection = Vector2.Dot((Vector2)vertex.position, dir);
                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }

            float range = max - min;
            float invRange = range > 0f ? 1f / range : 0f;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = (Vector2.Dot((Vector2)vertex.position, dir) - min) * invRange;
                vertex.color = ApplyBlend(vertex.color, EvaluateColor(t));
                vh.SetUIVertex(vertex, i);
            }
        }

        private Vector2 GetDirectionVector()
        {
            switch (direction)
            {
                case Direction.Horizontal:
                    return Vector2.right;
                case Direction.Vertical:
                    return Vector2.down; // start color at the top
                default:
                    float radians = angle * Mathf.Deg2Rad;
                    return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
        }

        private Color EvaluateColor(float t)
        {
            if (colorSource == ColorSource.Gradient && gradient != null)
            {
                return gradient.Evaluate(t);
            }

            return Color.Lerp(startColor, endColor, t);
        }

        private Color ApplyBlend(Color existing, Color gradientColor)
        {
            switch (blendMode)
            {
                case BlendMode.Override:
                    return gradientColor;
                case BlendMode.Additive:
                    return existing + gradientColor;
                default:
                    return existing * gradientColor;
            }
        }

        private void MarkDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        private static UnityEngine.Gradient CreateDefaultGradient()
        {
            var result = new UnityEngine.Gradient();
            result.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.black, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return result;
        }
    }
}
