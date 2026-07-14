using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UIWidgets
{
    public class WaitPanel : SingletonBehaviour<WaitPanel>
    {
        public GameObject waitPanel;
        public TextMeshProUGUI waitingText;
        public UnityEvent onClose;

        private int _count = 0;
    
        public void Show(string text="Please Wait..")
        {
		if (waitPanel == null)
		{
			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Panel] WaitPanel game object not assigned in {SceneName}", SceneManager.GetActiveScene().name);
		}
		else
		{
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] WaitPanel Shown in {SceneName}", SceneManager.GetActiveScene().name);
                if (waitingText != null)
                {
                    waitingText.text = text;
                }
                waitPanel.SetActive(true);
            }
        }
    
        public void ShowCounted(string text = "")
        {
		if (waitPanel == null)
		{
			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR:Panel] WaitPanel game object not assigned in {SceneName}", SceneManager.GetActiveScene().name);
		}
		else
		{
			_count++;
			DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] WaitPanel Shown in {SceneName} count={Count}", SceneManager.GetActiveScene().name, _count);
                if (waitingText != null)
                {
                    waitingText.text = text;
                }
                waitPanel.SetActive(true);
            }
        }

        public void HideCounted()
        {
            _count--;
            if (_count <= 0)
            {
                _count = 0;
                Hide();
            }
        }
    
	public void Hide()
	{
		DebugX.Logger(LogChannels.UI).Info("[UI:INFO:Panel] WaitPanel Hidden in {SceneName}", SceneManager.GetActiveScene().name);
            if (waitPanel != null)
            {
                waitPanel.SetActive(false);
            }
            onClose?.Invoke();
            onClose?.RemoveAllListeners();
        }
    }
}