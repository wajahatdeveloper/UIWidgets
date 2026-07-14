using AetherNexus.FoundationPlatform.DebugX;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor
{
	[CustomPropertyDrawer(typeof(ButtonReference))]
	public class ButtonReferenceDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty buttonObjectProp = property.FindPropertyRelative("buttonObject");
			if (buttonObjectProp == null)
			{
				EditorGUI.LabelField(position, label.text, "ButtonReference: buttonObject property not found");
				EditorGUI.EndProperty();
				return;
			}

			Object currentValue = buttonObjectProp.objectReferenceValue;
			
			// Use ObjectField that accepts MonoBehaviour (both Button and ButtonX inherit from MonoBehaviour)
			Object newValue = EditorGUI.ObjectField(
				position,
				label,
				currentValue,
				typeof(MonoBehaviour),
				true // allowSceneObjects
			);

			// Handle GameObject dragging - search for Button or ButtonX component
			if (newValue != null && newValue is GameObject gameObject)
			{
				Button button = gameObject.GetComponent<Button>();
				ButtonX buttonX = gameObject.GetComponent<ButtonX>();
				
				if (buttonX != null)
				{
					newValue = buttonX;
				}
				else if (button != null)
				{
					newValue = button;
				}
			else
			{
				DebugX.Logger(LogChannels.Editor).Warning("[UI:WARN:Editor] ButtonReference: '{GameObjectName}' has no Button or ButtonX.", gameObject.name);
				newValue = currentValue;
			}
			}
			// Handle Component dragging - if it's not Button/ButtonX, try to find Button/ButtonX on the same GameObject
			else if (newValue != null && newValue is Component component && !(newValue is Button) && !(newValue is ButtonX))
			{
				GameObject componentGameObject = component.gameObject;
				Button button = componentGameObject.GetComponent<Button>();
				ButtonX buttonX = componentGameObject.GetComponent<ButtonX>();
				
				if (buttonX != null)
				{
					newValue = buttonX;
				}
				else if (button != null)
				{
					newValue = button;
				}
			else
			{
				DebugX.Logger(LogChannels.Editor).Warning("[UI:WARN:Editor] ButtonReference: cannot assign {TypeName}. '{GameObjectName}' has no Button or ButtonX.", newValue.GetType().Name, componentGameObject.name);
				newValue = currentValue;
			}
			}
			// Validate that it's either Button or ButtonX (or null)
			else if (newValue != null && !(newValue is Button) && !(newValue is ButtonX))
			{
				// Revert to previous value if invalid type
				DebugX.Logger(LogChannels.Editor).Warning("[UI:WARN:Editor] ButtonReference: only Button or ButtonX allowed, got {TypeName}.", newValue.GetType().Name);
				newValue = currentValue;
			}

			if (buttonObjectProp.objectReferenceValue != newValue)
			{
				buttonObjectProp.objectReferenceValue = newValue;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}

