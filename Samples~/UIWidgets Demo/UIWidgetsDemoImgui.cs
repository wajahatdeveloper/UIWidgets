#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace AetherNexus.UIWidgets
{
    /// <summary>Shared narrow left-rail IMGUI layout for sample harness scripts.</summary>
    public static class UIWidgetsDemoImgui
    {
        public const float Left = 4f;
        public const float Width = 128f;
        public const float RowH = 18f;
        public const float Gap = 1f;

        // Vertical slots so all harnesses stack on the left without overlapping.
        public const float DialogY = 4f;
        public const float InputY = 90f;
        public const float FaderY = 156f;
        public const float LineY = 242f;
        public const float LoadingY = 308f;
        public const float WaitY = 374f;
        public const float ModalY = 460f;

        public static bool Button(ref float y, string label)
        {
            bool hit = GUI.Button(new Rect(Left, y, Width, RowH), label);
            y += RowH + Gap;
            return hit;
        }

        public static void Label(ref float y, string text)
        {
            GUI.Label(new Rect(Left, y, Width, RowH * 1.5f), text);
            y += RowH * 1.5f + Gap;
        }
    }
}
#endif
