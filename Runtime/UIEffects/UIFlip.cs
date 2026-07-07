using UnityEngine;
using UnityEngine.UI;

namespace UIWidgets
{
    /// <summary>
    /// Mirrors the attached <see cref="Graphic"/>'s mesh horizontally and/or vertically around
    /// the rect center, flipping the visual without touching the transform.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/UIWidgets/Flip")]
    public class UIFlip : BaseMeshEffect
    {
        [SerializeField]
        private bool horizontal;

        [SerializeField]
        private bool vertical;

        public bool Horizontal
        {
            get => horizontal;
            set
            {
                horizontal = value;
                MarkDirty();
            }
        }

        public bool Vertical
        {
            get => vertical;
            set
            {
                vertical = value;
                MarkDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || (!horizontal && !vertical))
            {
                return;
            }

            Vector2 center = ((RectTransform)transform).rect.center;
            var vertex = default(UIVertex);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);

                var position = vertex.position;
                if (horizontal)
                {
                    position.x = 2f * center.x - position.x;
                }

                if (vertical)
                {
                    position.y = 2f * center.y - position.y;
                }

                vertex.position = position;
                vh.SetUIVertex(vertex, i);
            }
        }

        private void MarkDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
