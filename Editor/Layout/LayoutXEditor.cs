using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Shows only the fields relevant to the selected <see cref="LayoutX"/> mode and constraint.
	/// </summary>
	[CustomEditor(typeof(LayoutX), true)]
	[CanEditMultipleObjects]
	public class LayoutXEditor : UnityEditor.Editor
	{
		SerializedProperty mode;
		SerializedProperty mainAxis;
		SerializedProperty constraint;
		SerializedProperty constraintCount;
		SerializedProperty spacing;
		SerializedProperty padding;
		SerializedProperty childAlignment;
		SerializedProperty lineAlignment;
		SerializedProperty crossAlignment;
		SerializedProperty childWidth;
		SerializedProperty childHeight;
		SerializedProperty useChildRectSize;
		SerializedProperty resetChildRotation;
		SerializedProperty reverseArrangement;
		SerializedProperty childForceExpandMain;
		SerializedProperty childForceExpandCross;

		void OnEnable()
		{
			mode = serializedObject.FindProperty("mode");
			mainAxis = serializedObject.FindProperty("mainAxis");
			constraint = serializedObject.FindProperty("constraint");
			constraintCount = serializedObject.FindProperty("constraintCount");
			spacing = serializedObject.FindProperty("spacing");
			padding = serializedObject.FindProperty("m_Padding");
			childAlignment = serializedObject.FindProperty("m_ChildAlignment");
			lineAlignment = serializedObject.FindProperty("lineAlignment");
			crossAlignment = serializedObject.FindProperty("crossAlignment");
			childWidth = serializedObject.FindProperty("childWidth");
			childHeight = serializedObject.FindProperty("childHeight");
			useChildRectSize = serializedObject.FindProperty("useChildRectSize");
			resetChildRotation = serializedObject.FindProperty("resetChildRotation");
			reverseArrangement = serializedObject.FindProperty("reverseArrangement");
			childForceExpandMain = serializedObject.FindProperty("childForceExpandMain");
			childForceExpandCross = serializedObject.FindProperty("childForceExpandCross");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(mode, new GUIContent("Mode", "Compact = flow wrap by size. Grid = uniform cells."));
			EditorGUILayout.PropertyField(mainAxis, new GUIContent("Main Axis", "Horizontal: flow right, wrap to next row. Vertical: flow down, wrap to next column."));
			EditorGUILayout.PropertyField(constraint, new GUIContent("Constraint", "Flexible wraps by available main length. Max Items / Max Lines cap per line."));

			if (constraint.enumValueIndex != (int)LayoutX.LineConstraint.Flexible)
			{
				EditorGUILayout.PropertyField(constraintCount, new GUIContent("Constraint Count"));
			}

			EditorGUILayout.PropertyField(spacing);
			EditorGUILayout.PropertyField(padding);
			EditorGUILayout.PropertyField(reverseArrangement, new GUIContent("Reverse Arrangement", "Lay out children in reverse hierarchy order."));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(childAlignment, new GUIContent("Child Alignment", "Anchors the whole content block inside this rect (LayoutGroup)."));
			EditorGUILayout.PropertyField(lineAlignment, new GUIContent("Line Alignment", "Aligns items along the main axis within each line."));
			EditorGUILayout.PropertyField(crossAlignment, new GUIContent("Cross Alignment", "Aligns items along the cross axis within each line."));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(childWidth, new GUIContent("Child Width", "None = keep rect width. Preferred = drive to preferred width."));
			EditorGUILayout.PropertyField(childHeight, new GUIContent("Child Height", "None = keep rect height. Preferred = drive to preferred height."));
			EditorGUILayout.PropertyField(childForceExpandMain, new GUIContent("Force Expand Main", "Distribute free main-axis space across items on each line (Grid Flexible grows cells)."));
			EditorGUILayout.PropertyField(childForceExpandCross, new GUIContent("Force Expand Cross", "Stretch children to line thickness (Grid Flexible grows cells)."));
			EditorGUILayout.PropertyField(useChildRectSize);
			EditorGUILayout.PropertyField(resetChildRotation);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
