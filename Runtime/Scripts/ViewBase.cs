using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	///  Base class for scene-specific view roots.
	///  Each scene should have its own concrete implementation (e.g., MainMenuView, CombatExampleView).
	///  Kept intentionally light; views compose their own references and lifecycle hooks
	///  (e.g. ISimulationStart). Modal coordination is handled independently by <see cref="ModalService"/>.
	/// </summary>
	public abstract class ViewBase : MonoBehaviour { }
}
