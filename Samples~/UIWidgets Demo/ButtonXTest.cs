#if UNITY_EDITOR || DEVELOPMENT_BUILD
using AetherNexus.FoundationPlatform.DebugX;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Play Mode harness for <see cref="ButtonX"/>.</summary>
	public class ButtonXTest : MonoBehaviour, IDemoImguiHarness
	{
		[Tooltip("Assign or let DemoGalleryBootstrap resolve Demo_ButtonX.")]
		public ButtonX demoButton;

		[Header("Toggle group (wire in scene)")]
		[Tooltip("One ButtonXToggleGroup shared by toggleGroupButtons.")]
		public ButtonXToggleGroup toggleGroup;
		[Tooltip("Members that share toggleGroup. Enable toggleMode + assign group in Inspector.")]
		public ButtonX[] toggleGroupButtons;

		private void OnEnable()
		{
			if (demoButton == null)
				demoButton = FindFirstObjectByType<ButtonX>();
			if (demoButton == null)
				return;

			demoButton.OnClicked.RemoveListener(OnClicked);
			demoButton.OnClicked.AddListener(OnClicked);
			demoButton.OnLongPressed.RemoveListener(OnLongPressed);
			demoButton.OnLongPressed.AddListener(OnLongPressed);
			demoButton.OnDoubleClicked.RemoveListener(OnDoubleClicked);
			demoButton.OnDoubleClicked.AddListener(OnDoubleClicked);
		}

		private void OnDisable()
		{
			if (demoButton == null)
				return;
			demoButton.OnClicked.RemoveListener(OnClicked);
			demoButton.OnLongPressed.RemoveListener(OnLongPressed);
			demoButton.OnDoubleClicked.RemoveListener(OnDoubleClicked);
		}

		private void OnClicked() =>
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ButtonX clicked.");

		private void OnLongPressed() =>
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ButtonX long-press.");

		private void OnDoubleClicked() =>
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ButtonX double-click.");

		[ContextMenu("Click")]
		public void TestClick()
		{
			if (demoButton == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign ButtonX (or run DemoGalleryBootstrap.Ensure Gallery).");
				return;
			}

			demoButton.Click();
		}

		[ContextMenu("Toggle Mode")]
		public void TestToggle()
		{
			if (demoButton == null)
				return;
			demoButton.toggleMode = !demoButton.toggleMode;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] ButtonX toggleMode={Mode}", demoButton.toggleMode);
		}

		[ContextMenu("Toggle Group/Select Next")]
		public void TestToggleGroupSelectNext()
		{
			if (toggleGroupButtons == null || toggleGroupButtons.Length == 0)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign toggleGroupButtons (and toggleGroup) in the Inspector.");
				return;
			}

			int current = -1;
			for (int i = 0; i < toggleGroupButtons.Length; i++)
			{
				var b = toggleGroupButtons[i];
				if (b != null && b.IsOn) { current = i; break; }
			}

			int next = (current + 1) % toggleGroupButtons.Length;
			var target = toggleGroupButtons[next];
			if (target == null)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] toggleGroupButtons[{Index}] is null.", next);
				return;
			}

			target.toggleMode = true;
			if (toggleGroup != null) { target.toggleGroup = toggleGroup; }
			target.SetIsOn(true);
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Toggle group selected {Name}", target.name);
		}

		[ContextMenu("Toggle Group/Apply Group Refs")]
		public void TestToggleGroupApplyRefs()
		{
			if (toggleGroup == null || toggleGroupButtons == null || toggleGroupButtons.Length == 0)
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Test] Assign toggleGroup + toggleGroupButtons first.");
				return;
			}

			for (int i = 0; i < toggleGroupButtons.Length; i++)
			{
				var b = toggleGroupButtons[i];
				if (b == null) { continue; }
				b.toggleMode = true;
				b.toggleGroup = toggleGroup;
				toggleGroup.Register(b);
			}

			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Test] Applied toggleGroup to {Count} buttons.", toggleGroupButtons.Length);
		}

		public void DrawImgui(ref float y)
		{
			UIWidgetsDemoImgui.Label(ref y, demoButton != null ? demoButton.name : "no ButtonX");
			if (UIWidgetsDemoImgui.Button(ref y, "BX Click")) TestClick();
			if (UIWidgetsDemoImgui.Button(ref y, "BX ToggleMode")) TestToggle();
			if (UIWidgetsDemoImgui.Button(ref y, "BX Group Next")) TestToggleGroupSelectNext();
			if (UIWidgetsDemoImgui.Button(ref y, "BX Group Apply Refs")) TestToggleGroupApplyRefs();
		}
	}
}
#endif
