using FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;

namespace UIWidgets
{
    public class LineMessage : SingletonBehaviour<LineMessage>
    {
        public GameObject messagePrefab;

        public void Show(string message, string titleString = "", float time = 4.0f)
        {
		if (messagePrefab == null)
		{
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Panel] LineMessage: messagePrefab not assigned.");
			return;
		}

		var messageLine = Instantiate(messagePrefab, transform);
		if (messageLine.transform.childCount < 2)
		{
			DebugX.Logger(LogChannels.UI).Warning("[UI:WARN:Panel] LineMessage: messagePrefab needs at least two children (title, message).");
		}
            var titleText = messageLine.transform.childCount > 0 ? messageLine.transform.GetChild(0).GetComponent<TextMeshProUGUI>() : null; // Title
            var messageText = messageLine.transform.childCount > 1 ? messageLine.transform.GetChild(1).GetComponent<TextMeshProUGUI>() : null; // Message
        
            if (messageText != null) messageText.text = message;
            if (titleText != null) titleText.text = titleString;
        
            Destroy(messageLine,time);
        }
    }
}