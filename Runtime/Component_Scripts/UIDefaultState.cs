#if UNITY_EDITOR
using System.Collections.Generic;
using FoundationPlatform.DebugX;
using FoundationPlatform.FrameworkInspector;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIWidgets
{
    [ExecuteInEditMode]
    public class UIDefaultState : MonoBehaviour
    {
        [LabelText("Enabled By Default (Assets + Scene)")]
        public List<GameObject> enabledByDefault = new();
        [Space]
        [LabelText("Disabled By Default (Assets + Scene)")]
        public List<GameObject> disabledByDefault = new();

        private void OnEnable()
        {
            EditorSceneManager.sceneClosing += Handle_EditorOnSceneClosing;
            EditorSceneManager.activeSceneChangedInEditMode += Handle_OnSceneChangedInEditMode;
        }

        private void Handle_OnSceneChangedInEditMode(Scene arg0, Scene arg1)
        {
            // Reset UI state when switching scenes in edit mode
            Apply();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneClosing -= Handle_EditorOnSceneClosing;
            EditorSceneManager.activeSceneChangedInEditMode -= Handle_OnSceneChangedInEditMode;
        }

        private void Handle_EditorOnSceneClosing(Scene scene, bool removingScene)
        {
            Apply();
        }

        [Button]
        [ContextMenu("Apply UI Default State")]
        private void Apply()
        {
            if (Application.isPlaying) { return; }

            bool isDirty = false;
            List<string> enabledObjects = new();
            List<string> disabledObjects = new();

            // Apply enabled by default objects
            if (enabledByDefault != null)
            {
                foreach (GameObject o in enabledByDefault)
                {
                    if (o == null) { continue; }
                    if (!o.activeSelf)
                    {
                        o.SetActive(true);
                        enabledObjects.Add(o.name);
                        isDirty = true;
                    }
                }
            }

            // Apply disabled by default objects
            if (disabledByDefault != null)
            {
                foreach (GameObject o in disabledByDefault)
                {
                    if (o == null) { continue; }
                    if (o.activeSelf)
                    {
                        o.SetActive(false);
                        disabledObjects.Add(o.name);
                        isDirty = true;
                    }
                }
            }

			if (isDirty)
			{
				EditorSceneManager.MarkAllScenesDirty();

				DebugX.Logger(LogChannels.Editor).Info("[UI:INFO:Editor] UIDefaultState updated.");

				if (enabledObjects.IsNotEmpty())
				{
					DebugX.Logger(LogChannels.Editor).Info("[UI:INFO:Editor] UIDefaultState enabled: \n{EnabledObjects}.", enabledObjects.StringJoin("\n"));
				}

				if (disabledObjects.IsNotEmpty())
				{
					DebugX.Logger(LogChannels.Editor).Info("[UI:INFO:Editor] UIDefaultState disabled: \n{DisabledObjects}.", disabledObjects.StringJoin("\n"));
				}

				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			}
        }
    }
}
#endif