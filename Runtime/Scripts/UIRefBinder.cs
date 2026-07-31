using System;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Marker field type: declare `[SerializeField] private UIRefBinder ...;` on any MonoBehaviour to
	/// get a "Bind UI From Selection" tool in that field's Inspector row (see UIRefBinderDrawer).
	/// Carries no runtime data.
	/// </summary>
	[Serializable]
	public class UIRefBinder { }
}
