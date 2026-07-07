using FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
    [CustomEditor(typeof(ScrollList), true)]
    [CanEditMultipleObjects]
    public class ScrollListEditor : FrameworkEditor
    {
        private SerializedProperty _itemPrefab;

        protected override void OnEnable()
        {
            base.OnEnable();
            _itemPrefab = serializedObject.FindProperty("itemPrefab");
        }

        public override void OnInspectorGUI()
        {
            // Engine draws the fields plus ScrollList's own [ShowIf]/[ShowInInspector] debug readouts and
            // [FoldoutGroup("Runtime Controls")] [Button] methods — no need to re-draw them here.
            base.OnInspectorGUI();

            // Unique tool that can't be expressed as an attribute: duplicate the item prefab to a new asset.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUI.BeginDisabledGroup(_itemPrefab == null || _itemPrefab.objectReferenceValue == null);
            if (GUILayout.Button("Duplicate item prefab and save…", GUILayout.Height(22)))
                DuplicateAndSaveItemPrefab();
            EditorGUI.EndDisabledGroup();

            if (_itemPrefab != null && _itemPrefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign an Item Prefab above to duplicate and save a copy.", MessageType.Info);
            }
            else if (_itemPrefab != null && _itemPrefab.objectReferenceValue != null &&
                     PrefabUtility.GetPrefabAssetType(_itemPrefab.objectReferenceValue) == PrefabAssetType.NotAPrefab)
            {
                EditorGUILayout.HelpBox("Item Prefab must be a prefab asset, not a scene object.", MessageType.Error);
            }
        }

        private void DuplicateAndSaveItemPrefab()
        {
            if (_itemPrefab == null || _itemPrefab.objectReferenceValue == null)
            {
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Item Prefab not assigned.");
                return;
            }

            var prefab = _itemPrefab.objectReferenceValue as GameObject;
            if (prefab == null)
            {
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Item Prefab must be GameObject.");
                return;
            }

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Item Prefab must be prefab asset.");
                return;
            }

            string defaultName = string.IsNullOrEmpty(prefab.name) ? "ItemPrefab" : prefab.name;
            string path = EditorUtility.SaveFilePanelInProject("Save item prefab", defaultName, "prefab", "duplicate prefab");
            if (string.IsNullOrEmpty(path))
                return;
            if (!path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                path += ".prefab";

            var tempParent = new GameObject("TempScrollableListPrefabDuplicate");
            tempParent.hideFlags = HideFlags.HideAndDontSave;
            GameObject instance = Object.Instantiate(prefab, tempParent.transform);
            if (instance == null)
            {
                DestroyImmediate(tempParent);
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Failed to instantiate prefab.");
                return;
            }

            if (!PrefabUtility.SaveAsPrefabAsset(instance, path))
            {
                DestroyImmediate(tempParent);
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Failed to save prefab to " + path);
                return;
            }

            DestroyImmediate(tempParent);

            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (newPrefab == null)
            {
                Debug.LogError("[UI:ERROR:Editor] ScrollList: Failed to load prefab at " + path);
                return;
            }

            Undo.RecordObjects(serializedObject.targetObjects, "Duplicate Item Prefab");
            _itemPrefab.objectReferenceValue = newPrefab;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
