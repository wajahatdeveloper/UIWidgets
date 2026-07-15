using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Default row payload for the stock <see cref="ScrollItemView"/> item prefab.
	/// Prefer a typed <see cref="ScrollItemView{T}"/> when binding domain models.
	/// </summary>
	public sealed class ScrollListItemData
	{
		public string Title;
		public string Subtitle;
		public Sprite Icon;

		public ScrollListItemData(string title, string subtitle = null, Sprite icon = null)
		{
			Title = title ?? string.Empty;
			Subtitle = subtitle ?? string.Empty;
			Icon = icon;
		}
	}
}
