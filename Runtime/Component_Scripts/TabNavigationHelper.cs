using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UIWidgets
{
    /// <summary>
    /// Enhanced tab navigation system with manual navigation paths and circular navigation support.
    /// </summary>
    public class TabNavigationHelper : MonoBehaviour
    {
        public enum TabNavigationMode { Auto = 0, Manual = 1 }
    
        [Header("Navigation Settings")]
        [Tooltip("The path to take when user is tabbing through UI components.")]
        public Selectable[] NavigationPath;
    
        [Tooltip("Use the default Unity navigation system or a manual fixed order using Navigation Path")]
        public TabNavigationMode NavigationMode = TabNavigationMode.Auto;
    
        [Tooltip("If True, this will loop the tab order from last to first automatically")]
        public bool CircularNavigation = true;
    
        [Tooltip("If True, automatically select the first element when no element is selected")]
        public bool AutoSelectFirst = true;
    
        private EventSystem _system;
        private Selectable startingObject;
        private Selectable lastObject;
        private GameObject _lastDiscoveryAnchor;
 
        void Start()
        {
		_system = EventSystem.current;
		if (_system == null)
		{
			DebugX.Logger(LogChannels.UI).Error("[UI:ERROR] TabSwitchFocus needs EventSystem in scene.");
			return;
		}
        
            if (NavigationMode == TabNavigationMode.Manual && NavigationPath.Length > 0)
            {
                startingObject = NavigationPath[0].gameObject.GetComponent<Selectable>();
            }
        
            if (startingObject == null && CircularNavigation)
            {
                SelectDefaultObject(out startingObject); 
            }
        }
        
        private void Update()
        {
            if (_system == null) { _system = EventSystem.current; }
            if (_system == null) return;
        
            Selectable next = null;
        
            // Find the last selectable object for circular navigation.
            // Only run the discovery walk once per selection anchor to avoid
            // allocating a new Stack and re-walking FindSelectableOnDown every frame.
            if (lastObject == null
                && _system.currentSelectedGameObject != null
                && _lastDiscoveryAnchor != _system.currentSelectedGameObject)
            {
                _lastDiscoveryAnchor = _system.currentSelectedGameObject;
                var startingPoint = _system.currentSelectedGameObject.GetComponent<Selectable>();
                var selectableItems = new Stack<Selectable>();
                selectableItems.Push(startingPoint);

                // Find the last selectable object
                next = startingPoint.FindSelectableOnDown();
                while (next != null)
                {
                    if (selectableItems.Contains(next))
                    {
                        lastObject = selectableItems.Pop();
                        selectableItems.Clear();
                        break;
                    }
                    lastObject = next;
                    selectableItems.Push(next);
                    next = next.FindSelectableOnDown();
                }
            }
        
            // Handle Tab key navigation (New Input System)
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                bool isShiftHeld = (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            
                if (NavigationMode == TabNavigationMode.Manual && NavigationPath.Length > 0)
                {
                    next = GetNextFromManualPath(isShiftHeld);
                }
                else
                {
                    next = GetNextFromAutoNavigation(isShiftHeld);
                }
            }
            else if (_system.currentSelectedGameObject == null && AutoSelectFirst)
            {
                SelectDefaultObject(out next);
            }
        
            if (CircularNavigation && startingObject == null)
            {
                startingObject = next;
            }
        
            SelectGameObject(next);
        }
    
        private Selectable GetNextFromManualPath(bool isShiftHeld)
        {
            if (NavigationPath.Length == 0) return null;
        
            var currentGameObject = _system.currentSelectedGameObject;
            if (currentGameObject == null) return NavigationPath[0];
        
            for (var i = 0; i < NavigationPath.Length; i++)
            {
                if (_system.currentSelectedGameObject != NavigationPath[i].gameObject) continue;
            
                if (isShiftHeld)
                {
                    return i == 0 ? NavigationPath[NavigationPath.Length - 1] : NavigationPath[i - 1];
                }
                else
                {
                    return i == (NavigationPath.Length - 1) ? NavigationPath[0] : NavigationPath[i + 1];
                }
            }
        
            return isShiftHeld ? NavigationPath[NavigationPath.Length - 1] : NavigationPath[0];
        }
    
        private Selectable GetNextFromAutoNavigation(bool isShiftHeld)
        {
            var currentGameObject = _system.currentSelectedGameObject;
            if (currentGameObject == null)
            {
                SelectDefaultObject(out Selectable defaultNext);
                return defaultNext;
            }
        
            var currentSelectable = currentGameObject.GetComponent<Selectable>();
            if (currentSelectable == null) return null;
        
            Selectable next = isShiftHeld ? currentSelectable.FindSelectableOnUp() : currentSelectable.FindSelectableOnDown();
        
            if (next == null && CircularNavigation)
            {
                next = isShiftHeld ? lastObject : startingObject;
            }
        
            return next;
        }
    
        private void SelectDefaultObject(out Selectable next)
        {
            if (_system.firstSelectedGameObject)
            {
                next = _system.firstSelectedGameObject.GetComponent<Selectable>();
            }
            else
            {
                next = null;
            }
        }
    
        private void SelectGameObject(Selectable selectable)
        {
            if (selectable == null) return;
        
            // Handle InputField (legacy)
            InputField inputfield = selectable.GetComponent<InputField>();
            if (inputfield != null)
            {
                inputfield.OnPointerClick(new PointerEventData(_system));
                _system.SetSelectedGameObject(selectable.gameObject, new BaseEventData(_system));
                return;
            }
        
            // Handle TMP_InputField
            TMP_InputField tmpInputField = selectable.GetComponent<TMP_InputField>();
            if (tmpInputField != null)
            {
                tmpInputField.OnPointerClick(new PointerEventData(_system));
                _system.SetSelectedGameObject(selectable.gameObject, new BaseEventData(_system));
                return;
            }
        
            // Handle other selectables
            _system.SetSelectedGameObject(selectable.gameObject, new BaseEventData(_system));
        }
    
        /// <summary>
        /// Manually set the navigation path at runtime
        /// </summary>
        public void SetNavigationPath(Selectable[] newPath)
        {
            NavigationPath = newPath;
            if (NavigationPath.Length > 0)
            {
                startingObject = NavigationPath[0];
            }
        }
    
        /// <summary>
        /// Add a selectable to the navigation path
        /// </summary>
        public void AddToNavigationPath(Selectable selectable)
        {
            if (selectable == null) return;
        
            var newPath = new List<Selectable>();
            if (NavigationPath != null)
            {
                newPath.AddRange(NavigationPath);
            }
            newPath.Add(selectable);
            NavigationPath = newPath.ToArray();
        }
    
        /// <summary>
        /// Remove a selectable from the navigation path
        /// </summary>
        public void RemoveFromNavigationPath(Selectable selectable)
        {
            if (NavigationPath == null || selectable == null) return;
        
            var newPath = new List<Selectable>();
            foreach (var item in NavigationPath)
            {
                if (item != selectable)
                {
                    newPath.Add(item);
                }
            }
            NavigationPath = newPath.ToArray();
        }
    }
}
