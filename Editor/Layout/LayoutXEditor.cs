using UnityEditor;

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
		SerializedProperty vertical;
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

		void OnEnable()
		{
			mode = serializedObject.FindProperty("mode");
			vertical = serializedObject.FindProperty("vertical");
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
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(mode);
			EditorGUILayout.PropertyField(vertical);
			EditorGUILayout.PropertyField(constraint);

			if (constraint.enumValueIndex != (int)LayoutX.LineConstraint.Flexible)
			{
				EditorGUILayout.PropertyField(constraintCount);
			}

			EditorGUILayout.PropertyField(spacing);
			EditorGUILayout.PropertyField(padding);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(childAlignment);
			EditorGUILayout.PropertyField(lineAlignment);
			EditorGUILayout.PropertyField(crossAlignment);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Children", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(childWidth);
			EditorGUILayout.PropertyField(childHeight);
			EditorGUILayout.PropertyField(useChildRectSize);
			EditorGUILayout.PropertyField(resetChildRotation);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
