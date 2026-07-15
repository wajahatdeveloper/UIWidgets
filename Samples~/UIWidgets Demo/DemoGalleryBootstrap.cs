#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Draws section tabs, left-rail IMGUI (single OnGUI — avoids multi-script control-ID bugs),
	/// and toggles scene demo roots so only the active section is visible.
	/// All gallery widgets / singletons are expected to already exist in the sample scene.
	/// </summary>
	public class DemoGalleryBootstrap : MonoBehaviour
	{
		[Header("Scene instances")]
		[SerializeField] private ButtonX demoButtonX;
		[SerializeField] private ButtonXToggleGroup demoToggleGroup;
		[SerializeField] private ScrollList demoScrollList;
		[SerializeField] private UITabs demoTabs;
		[SerializeField] private LayoutX demoLayoutX;

		[Header("Section roots (disabled until section shown)")]
		[Tooltip("Optional — e.g. ModalPanel. Leave empty to skip.")]
		[SerializeField] private GameObject[] modalsSectionRoots;
		[SerializeField] private GameObject[] buttonsSectionRoots;
		[SerializeField] private GameObject[] listsSectionRoots;
		[SerializeField] private GameObject[] feedbackSectionRoots;
		[SerializeField] private GameObject[] layoutSectionRoots;

		[Header("Harness wiring")]
		[SerializeField] private DialogTest dialogTest;
		[SerializeField] private InputDialogTest inputDialogTest;
		[SerializeField] private FaderTest faderTest;
		[SerializeField] private LineMessageTest lineMessageTest;
		[SerializeField] private LoadingPanelTest loadingPanelTest;
		[SerializeField] private WaitPanelTest waitPanelTest;
		[SerializeField] private ModalServiceTest modalServiceTest;
		[SerializeField] private ButtonXTest buttonXTest;
		[SerializeField] private ScrollListTest scrollListTest;
		[SerializeField] private TabsTest tabsTest;
		[SerializeField] private ToastTest toastTest;
		[SerializeField] private ContextMenuTest contextMenuTest;
		[SerializeField] private PopupTextTest popupTextTest;
		[SerializeField] private LayoutXTest layoutXTest;

		private void Awake()
		{
			ResolveSceneInstances();
			ResolveHarnesses();
			WireHarnesses();
			CollectDefaultSectionRoots();
			ApplySectionVisibility(UIWidgetsDemoImgui.CurrentSection);
		}

		private void OnGUI()
		{
			UIWidgetsDemoImgui.DrawSectionTabs();
			if (UIWidgetsDemoImgui.ConsumeSectionChanged())
				ApplySectionVisibility(UIWidgetsDemoImgui.CurrentSection);

			float y = UIWidgetsDemoImgui.BeginContent();
			switch (UIWidgetsDemoImgui.CurrentSection)
			{
				case UIWidgetsDemoImgui.Section.Modals:
					DrawBlock(ref y, dialogTest);
					DrawBlock(ref y, inputDialogTest);
					DrawBlock(ref y, faderTest);
					DrawBlock(ref y, lineMessageTest);
					DrawBlock(ref y, loadingPanelTest);
					DrawBlock(ref y, waitPanelTest);
					DrawBlock(ref y, modalServiceTest);
					break;
				case UIWidgetsDemoImgui.Section.Buttons:
					DrawBlock(ref y, buttonXTest);
					break;
				case UIWidgetsDemoImgui.Section.Lists:
					DrawBlock(ref y, scrollListTest);
					DrawBlock(ref y, tabsTest);
					break;
				case UIWidgetsDemoImgui.Section.Feedback:
					DrawBlock(ref y, toastTest);
					DrawBlock(ref y, contextMenuTest);
					DrawBlock(ref y, popupTextTest);
					break;
				case UIWidgetsDemoImgui.Section.Layout:
					DrawBlock(ref y, layoutXTest);
					break;
			}

			UIWidgetsDemoImgui.EndContent(y);
		}

		private static void DrawBlock(ref float y, IDemoImguiHarness harness)
		{
			if (harness == null)
				return;
			harness.DrawImgui(ref y);
			UIWidgetsDemoImgui.BlockSpacer(ref y);
		}

		[ContextMenu("Resolve Scene + Apply Visibility")]
		public void ResolveAndApply()
		{
			ResolveSceneInstances();
			ResolveHarnesses();
			WireHarnesses();
			CollectDefaultSectionRoots();
			ApplySectionVisibility(UIWidgetsDemoImgui.CurrentSection);
		}

		private void ResolveSceneInstances()
		{
			if (demoButtonX == null)
				demoButtonX = FindFirstObjectByType<ButtonX>(FindObjectsInactive.Include);
			if (demoToggleGroup == null)
				demoToggleGroup = FindFirstObjectByType<ButtonXToggleGroup>(FindObjectsInactive.Include);
			if (demoScrollList == null)
				demoScrollList = FindFirstObjectByType<ScrollList>(FindObjectsInactive.Include);
			if (demoTabs == null)
				demoTabs = FindFirstObjectByType<UITabs>(FindObjectsInactive.Include);
			if (demoLayoutX == null)
				demoLayoutX = FindFirstObjectByType<LayoutX>(FindObjectsInactive.Include);
		}

		private void ResolveHarnesses()
		{
			if (dialogTest == null) dialogTest = FindFirstObjectByType<DialogTest>(FindObjectsInactive.Include);
			if (inputDialogTest == null) inputDialogTest = FindFirstObjectByType<InputDialogTest>(FindObjectsInactive.Include);
			if (faderTest == null) faderTest = FindFirstObjectByType<FaderTest>(FindObjectsInactive.Include);
			if (lineMessageTest == null) lineMessageTest = FindFirstObjectByType<LineMessageTest>(FindObjectsInactive.Include);
			if (loadingPanelTest == null) loadingPanelTest = FindFirstObjectByType<LoadingPanelTest>(FindObjectsInactive.Include);
			if (waitPanelTest == null) waitPanelTest = FindFirstObjectByType<WaitPanelTest>(FindObjectsInactive.Include);
			if (modalServiceTest == null) modalServiceTest = FindFirstObjectByType<ModalServiceTest>(FindObjectsInactive.Include);
			if (buttonXTest == null) buttonXTest = FindFirstObjectByType<ButtonXTest>(FindObjectsInactive.Include);
			if (scrollListTest == null) scrollListTest = FindFirstObjectByType<ScrollListTest>(FindObjectsInactive.Include);
			if (tabsTest == null) tabsTest = FindFirstObjectByType<TabsTest>(FindObjectsInactive.Include);
			if (toastTest == null) toastTest = FindFirstObjectByType<ToastTest>(FindObjectsInactive.Include);
			if (contextMenuTest == null) contextMenuTest = FindFirstObjectByType<ContextMenuTest>(FindObjectsInactive.Include);
			if (popupTextTest == null) popupTextTest = FindFirstObjectByType<PopupTextTest>(FindObjectsInactive.Include);
			if (layoutXTest == null) layoutXTest = FindFirstObjectByType<LayoutXTest>(FindObjectsInactive.Include);
		}

		private void CollectDefaultSectionRoots()
		{
			if (buttonsSectionRoots == null || buttonsSectionRoots.Length == 0)
			{
				var list = new System.Collections.Generic.List<GameObject>();
				if (demoButtonX != null)
					list.Add(demoButtonX.gameObject);
				if (demoToggleGroup != null)
					list.Add(demoToggleGroup.gameObject);
				if (list.Count > 0)
					buttonsSectionRoots = list.ToArray();
			}

			if (listsSectionRoots == null || listsSectionRoots.Length == 0)
			{
				var list = new System.Collections.Generic.List<GameObject>();
				if (demoScrollList != null)
					list.Add(demoScrollList.gameObject);
				if (demoTabs != null)
					list.Add(demoTabs.gameObject);
				if (list.Count > 0)
					listsSectionRoots = list.ToArray();
			}

			if (layoutSectionRoots == null || layoutSectionRoots.Length == 0)
			{
				if (demoLayoutX != null)
					layoutSectionRoots = new[] { demoLayoutX.gameObject };
			}

			if (feedbackSectionRoots == null || feedbackSectionRoots.Length == 0)
			{
				var list = new System.Collections.Generic.List<GameObject>();
				var toast = FindFirstObjectByType<ToastUI>(FindObjectsInactive.Include);
				if (toast != null)
					list.Add(toast.gameObject);
				var ctx = FindFirstObjectByType<ContextMenuWidget>(FindObjectsInactive.Include);
				if (ctx != null)
					list.Add(ctx.gameObject);
				// PopupText stays active (world-space; section toggle hides nothing useful and can break singleton timing).
				if (list.Count > 0)
					feedbackSectionRoots = list.ToArray();
			}
		}

		private void ApplySectionVisibility(UIWidgetsDemoImgui.Section section)
		{
			SetRootsActive(modalsSectionRoots, section == UIWidgetsDemoImgui.Section.Modals);
			SetRootsActive(buttonsSectionRoots, section == UIWidgetsDemoImgui.Section.Buttons);
			SetRootsActive(listsSectionRoots, section == UIWidgetsDemoImgui.Section.Lists);
			SetRootsActive(feedbackSectionRoots, section == UIWidgetsDemoImgui.Section.Feedback);
			SetRootsActive(layoutSectionRoots, section == UIWidgetsDemoImgui.Section.Layout);
		}

		private static void SetRootsActive(GameObject[] roots, bool active)
		{
			if (roots == null)
				return;
			for (int i = 0; i < roots.Length; i++)
			{
				if (roots[i] != null)
					roots[i].SetActive(active);
			}
		}

		private void WireHarnesses()
		{
			if (buttonXTest != null && demoButtonX != null)
				buttonXTest.demoButton = demoButtonX;
			if (buttonXTest != null && demoToggleGroup != null)
			{
				buttonXTest.toggleGroup = demoToggleGroup;
				if (buttonXTest.toggleGroupButtons == null || buttonXTest.toggleGroupButtons.Length == 0)
				{
					buttonXTest.toggleGroupButtons = demoToggleGroup.GetComponentsInChildren<ButtonX>(true);
				}
			}
			if (scrollListTest != null && demoScrollList != null)
				scrollListTest.scrollList = demoScrollList;
			if (tabsTest != null && demoTabs != null)
				tabsTest.tabs = demoTabs;
			if (layoutXTest != null && demoLayoutX != null)
				layoutXTest.layoutX = demoLayoutX;
		}
	}

	/// <summary>Drawn from <see cref="DemoGalleryBootstrap"/> OnGUI only (single IMGUI owner).</summary>
	public interface IDemoImguiHarness
	{
		void DrawImgui(ref float y);
	}
}
#endif
