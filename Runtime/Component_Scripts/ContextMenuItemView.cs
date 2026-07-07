using System;
using FoundationPlatform.DebugX;
using UnityEngine;

namespace UIWidgets
{
    [DisallowMultipleComponent]
    public sealed class ContextMenuItemView : MonoBehaviour
    {
        [SerializeField] private ButtonX button;

        public ButtonX Button => button;

        public void Bind(string text, bool isEnabled, Action onClick)
        {
            if (button == null)
            {
                DebugX.Builder(LogChannels.UI).WithContext(this).Error("[ContextMenuItemView] ButtonX reference is missing.");
                return;
            }

            button.OnClicked.RemoveAllListeners();
            button.OnClicked.AddListener(() => onClick?.Invoke());
            button.SetInteractable(isEnabled);
            button.SetText(text);
        }

        public void ClearBinding()
        {
            if (button == null)
            {
                return;
            }

            button.OnClicked.RemoveAllListeners();
        }
    }
}
