using System;
using UnityEngine;

namespace AetherNexus.UIWidgets
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

		public int Count => colors != null ? colors.Length : 0;

		public Color GetColor(int index)
		{
			if (colors == null || index < 0 || index >= colors.Length)
			{
				throw new InvalidOperationException(
					$"[UI:ERROR] ToastColorPalette '{name}' has no color at index {index} (count {Count}).");
			}
			return colors[index];
		}
	}
}
