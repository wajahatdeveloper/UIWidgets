#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor.UIRefBinder
{
	[CustomPropertyDrawer(typeof(AetherNexus.UIWidgets.UIRefBinder))]
	internal sealed class UIRefBinderDrawer : PropertyDrawer
	{
		// Reserves no space of its own; the EditorGUILayout calls inside Draw() append directly
		// to the ongoing layout stream. Classic Rect-based OnGUI (not CreatePropertyGUI) is required
		// here because parent inspectors in this codebase (PanelBaseEditor, the default MonoBehaviour
		// inspector for ViewBase) are IMGUI-based and never invoke CreatePropertyGUI.
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 0f;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			UIRefBinderInspectorSection.Draw(property);
		}
	}
}
#endif
