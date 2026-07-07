using UnityEngine;

namespace UIWidgets
{
    /// <summary>
    /// Shareable Toast color set indexed by the ToastColor enum order. Assign one on a ToastUI to swap the whole
    /// palette (brand colors, themes) from a single asset instead of editing colors on every Toast prefab.
    /// </summary>
    [CreateAssetMenu(fileName = "ToastColorPalette", menuName = "UI Widgets/Toast Color Palette", order = 200)]
    public sealed class ToastColorPalette : ScriptableObject
    {
        [Tooltip("Colors indexed by the ToastColor enum order.")]
        [SerializeField] private Color[] colors;

        public int Count => colors?.Length ?? 0;

        public Color GetColor(int index) =>
            (colors != null && index >= 0 && index < colors.Length) ? colors[index] : Color.white;
    }
}
