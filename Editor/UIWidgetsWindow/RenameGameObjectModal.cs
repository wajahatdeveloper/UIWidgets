using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
	public class RenameGameObjectModal : EditorWindow
	{
		private string prefix = "";
		private GameObject objectToRename;

		private string newName = "NewName";
		private bool buttonPressed = false;

		// Opens the modal dialog
		public static void Open(GameObject gameObject, string prefixStr)
		{
			RenameGameObjectModal window = GetWindow<RenameGameObjectModal>("Rename GameObject", true);
			window.prefix = prefixStr;
			window.objectToRename = gameObject;
			window.ShowModalUtility();
		}

		private void OnGUI()
		{
			// Check for Enter key press
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
			{
				buttonPressed = true;
				Event.current.Use(); // Consume the event so it doesn't get processed elsewhere
			}

			GUILayout.Label("Rename GameObject", EditorStyles.boldLabel);

			GUI.SetNextControlName("inputField");
			// Input field for new GameObject name
			newName = EditorGUILayout.TextField("New Name", newName);

			// Buttons for actions
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Rename") || buttonPressed)
			{
				buttonPressed = false;
				RenameSelectedGameObject();
				Close(); // Closes the window after renaming
			}

			if (GUILayout.Button("Cancel"))
			{
				GUI.FocusControl("");
				Close(); // Closes the window without renaming
			}

			GUILayout.EndHorizontal();
		}

		private void RenameSelectedGameObject()
		{
			if (objectToRename != null)
			{
				Undo.RecordObject(objectToRename, "Rename GameObject");
				objectToRename.name = prefix + newName;
			}
			else
			{
				EditorUtility.DisplayDialog("No Selection", "Please select a GameObject to rename.", "OK");
			}
		}
	}
}