using System.Collections.Generic;
using FoundationPlatform.FrameworkInspector;
using UnityEngine;
using UnityEngine.Events;

namespace UIWidgets
{
	public class UITabs : MonoBehaviour
	{
		[Header("References")]
		public Transform tabButtonParent;
		public Transform tabPanelParent;

		[ReadOnly] public ButtonX currentButton = null;
		[ReadOnly] public GameObject currentPanel = null;

		[Header("Settings")]
		[Tooltip("Index of the tab to select on initialize/enable.")]
		public int defaultTabIndex = 0;

		[Header("Events")]
		[Tooltip("Invoked when the active tab changes. Parameter is the new tab index.")]
		public UnityEvent<int> OnTabChange;

		private List<ButtonX> tabButtons = new();
		private List<GameObject> tabPanels = new();
		// Parallel to tabButtons: the exact UnityAction instances registered on each
		// button's OnClicked event, so the same delegate can be passed to RemoveListener.
		private List<UnityAction> tabButtonHandlers = new();
		private int currentIndex = -1;

#if UNITY_EDITOR
		[PropertyRange(0, "@panelCount - 1"), OnValueChanged(nameof(OnChange_CurrentPanelIndex))]
		public int currentPanelIndex = 0;
		private int panelCount = 1;

		[Button("Previous")]
		private void Previous()
		{
			if (currentPanelIndex > 0) { currentPanelIndex--; OnChange_CurrentPanelIndex(currentPanelIndex); }
		}

		[Button("Next")]
		private void Next()
		{
			if (currentPanelIndex < panelCount - 1) { currentPanelIndex++; OnChange_CurrentPanelIndex(currentPanelIndex); }
		}

#endif

		private void Awake()
		{
			// Ensure tabs are built at runtime as OnValidate is editor-only
			if ((tabButtons == null || tabButtons.Count == 0) || (tabPanels == null || tabPanels.Count == 0))
			{
				RebuildTabsFromParents();
			}
		}

		private void OnEnable()
		{
			RegisterButtonListeners();
			// Select default tab when enabling if nothing is selected yet
			if (currentButton == null || currentPanel == null)
			{
				SafeSelectTab(defaultTabIndex);
			}
		}

		private void OnDisable()
		{
			UnregisterButtonListeners();
		}

		public void OnClick_TabButton(int index)
		{
			if (!HasValidIndex(index)) { return; }

			int previousIndex = currentIndex;
			if (previousIndex == index) { return; }

			if (currentPanel != null) { currentPanel.SetActive(false); }
			if (currentButton != null)
			{
				currentButton.toggleMode = true;
				currentButton.SetIsOn(false);
			}

			currentButton = tabButtons[index];
			currentPanel = tabPanels[index];

			if (currentButton != null)
			{
				currentButton.toggleMode = true;
				currentButton.SetIsOn(true);
			}
			if (currentPanel != null)
			{
				currentPanel.SetActive(true);
			}

			currentIndex = index;
			OnTabChange?.Invoke(index);
		}

		public void RebuildTabsFromParents()
		{
			tabButtons.Clear();
			tabPanels.Clear();

			if (tabButtonParent != null)
			{
				foreach (Transform child in tabButtonParent)
				{
					ButtonX button = child.GetComponent<ButtonX>();
					if (button == null) { continue; }
					tabButtons.Add(button);
				}
			}

			if (tabPanelParent != null)
			{
				foreach (Transform child in tabPanelParent)
				{
					GameObject panel = child.gameObject;
					tabPanels.Add(panel);
				}
			}

			UnregisterButtonListeners();
			RegisterButtonListeners();
			SafeSelectTab(defaultTabIndex);
		}

		public void SafeSelectTab(int index)
		{
			if (!HasAnyTabs()) { currentButton = null; currentPanel = null; return; }
			int clamped = Mathf.Clamp(index, 0, Mathf.Min(tabButtons.Count, tabPanels.Count) - 1);
			OnClick_TabButton(clamped);
		}

		public void SelectTabByName(string buttonOrPanelName)
		{
			if (!HasAnyTabs() || string.IsNullOrEmpty(buttonOrPanelName)) { return; }
			for (int i = 0; i < Mathf.Min(tabButtons.Count, tabPanels.Count); i++)
			{
				var b = tabButtons[i];
				var p = tabPanels[i];
				if ((b != null && b.name == buttonOrPanelName) || (p != null && p.name == buttonOrPanelName))
				{
					OnClick_TabButton(i);
					return;
				}
			}
		}

		private void RegisterButtonListeners()
		{
			// Remove any previously registered handlers first so listeners do not
			// accumulate across Awake/OnEnable/RebuildTabsFromParents calls.
			UnregisterButtonListeners();
			for (var i = 0; i < tabButtons.Count; i++)
			{
				var tabButton = tabButtons[i];
				if (tabButton == null) { tabButtonHandlers.Add(null); continue; }
				int capturedIndex = i;
				// Store the exact delegate instance so RemoveListener can match it later.
				UnityAction handler = () => OnClick_TabButton(capturedIndex);
				tabButtonHandlers.Add(handler);
				tabButton.OnClicked.AddListener(handler);
			}
		}

		private void UnregisterButtonListeners()
		{
			for (var i = 0; i < tabButtonHandlers.Count; i++)
			{
				var handler = tabButtonHandlers[i];
				if (handler == null) { continue; }
				if (i < tabButtons.Count)
				{
					var tabButton = tabButtons[i];
					if (tabButton != null && tabButton.OnClicked != null)
					{
						tabButton.OnClicked.RemoveListener(handler);
					}
				}
			}
			tabButtonHandlers.Clear();
		}

		private bool HasValidIndex(int index)
		{
			if (!HasAnyTabs()) { return false; }
			int max = Mathf.Min(tabButtons.Count, tabPanels.Count);
			return index >= 0 && index < max;
		}

		private bool HasAnyTabs()
		{
			return tabButtons != null && tabPanels != null && tabButtons.Count > 0 && tabPanels.Count > 0;
		}

#if UNITY_EDITOR
		private void OnChange_CurrentPanelIndex(int index)
		{
			OnClick_TabButton(index);
		}

		private void OnValidate()
		{
			panelCount = 0;

			tabButtons.Clear();
			tabPanels.Clear();

			if (tabButtonParent != null)
			{
				foreach (Transform child in tabButtonParent)
				{
					ButtonX button = child.GetComponent<ButtonX>();
					if (button == null) { continue; }
					tabButtons.Add(button);
				}
			}

			if (tabPanelParent != null)
			{
				foreach (Transform child in tabPanelParent)
				{
					GameObject panel = child.gameObject;
					tabPanels.Add(panel);
					panelCount++;
				}
			}

			// Defer tab selection: mutating active state / invoking events directly
			// in OnValidate is discouraged (runs on load/import). Run after validation.
			int clamped = Mathf.Clamp(currentPanelIndex, 0, Mathf.Max(0, panelCount - 1));
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if (this == null) { return; }
				OnClick_TabButton(clamped);
			};
		}
#endif
	}
}