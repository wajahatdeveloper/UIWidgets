using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor
{
    /// <summary>
    /// Editor tool to migrate legacy UnityEngine.UI.Text components to TextMeshProUGUI.
    /// Duplicates original GameObjects to preserve references and properly maps all properties.
    /// </summary>
    public static class TextToTMPMigrationTool
    {
        private const string MenuPath = "GameObject/UI (Canvas)/Upgrade Text to TextMeshProUGUI";

        [MenuItem(MenuPath, false, 30)]
        private static void MigrateSelectedObjects()
        {
            var selectedObjects = Selection.gameObjects;
            
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("TMP Migration", "No GameObjects selected.", "OK");
                return;
            }

            // Filter to objects with Text components
            var textObjects = selectedObjects
                .Where(go => go.GetComponent<Text>() != null)
                .ToList();

            if (textObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("TMP Migration", 
                    "No selected GameObjects have a Text component.", "OK");
                return;
            }

            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            try
            {
                for (int i = 0; i < textObjects.Count; i++)
                {
                    var go = textObjects[i];
                    
                    EditorUtility.DisplayProgressBar("Migrating Text to TMP", 
                        $"Processing: {go.name}", (float)i / textObjects.Count);

                    var result = MigrateTextComponent(go);
                    
                    if (result.success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        errors.Add($"• {go.name}: {result.error}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Show summary
            string message = $"Migration Complete!\n\nSuccess: {successCount}\nFailed: {failCount}";
            
            if (errors.Count > 0)
            {
                message += "\n\nErrors:\n" + string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                {
                    message += $"\n... and {errors.Count - 10} more errors (see Console)";
                    foreach (var error in errors.Skip(10))
                    {
                        Debug.LogError($"[TMP Migration] {error}");
                    }
                }
            }

            EditorUtility.DisplayDialog("TMP Migration", message, "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateMigrateSelectedObjects()
        {
            // Enable menu item only if at least one selected object has Text component
            return Selection.gameObjects != null && 
                   Selection.gameObjects.Any(go => go.GetComponent<Text>() != null);
        }

        /// <summary>
        /// Migrates a single GameObject's Text component to TextMeshProUGUI.
        /// </summary>
        private static (bool success, string error) MigrateTextComponent(GameObject go)
        {
            var textComponent = go.GetComponent<Text>();
            
            if (textComponent == null)
            {
                return (false, "No Text component found");
            }

            // Store all properties before any modifications
            var props = CaptureTextProperties(textComponent);

            // Find TMP font asset
            var tmpFont = FindTMPFontAsset(props.font);
            
            if (tmpFont == null)
            {
                string fontName = props.font != null ? props.font.name : "null";
                return (false, $"TMP font asset '{fontName} SDF' not found");
            }

            // Record undo for the entire operation
            Undo.SetCurrentGroupName($"Migrate {go.name} to TMP");
            int undoGroup = Undo.GetCurrentGroup();

            try
            {
                // Step 1: Create legacy duplicate
                var legacyDuplicate = DuplicateAsLegacy(go);
                Undo.RegisterCreatedObjectUndo(legacyDuplicate, "Create Legacy Duplicate");

                // Step 2: Remove existing TMP component if present
                var existingTMP = go.GetComponent<TextMeshProUGUI>();
                if (existingTMP != null)
                {
                    Undo.DestroyObjectImmediate(existingTMP);
                }

                // Step 3: Remove the Text component from original
                Undo.DestroyObjectImmediate(textComponent);

                // Step 4: Add TextMeshProUGUI component
                var tmpComponent = Undo.AddComponent<TextMeshProUGUI>(go);

                // Step 5: Apply all properties
                ApplyPropertiesToTMP(tmpComponent, props, tmpFont);

                // Mark scene as dirty
                EditorUtility.SetDirty(go);
                
                Undo.CollapseUndoOperations(undoGroup);

                Debug.Log($"[TMP Migration] Successfully migrated '{go.name}'. Legacy backup: '{legacyDuplicate.name}'");
                return (true, null);
            }
            catch (System.Exception ex)
            {
                Undo.CollapseUndoOperations(undoGroup);
                Undo.PerformUndo();
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Captures all relevant properties from a Text component.
        /// </summary>
        private static TextProperties CaptureTextProperties(Text text)
        {
            return new TextProperties
            {
                text = text.text,
                font = text.font,
                fontSize = text.fontSize,
                fontStyle = text.fontStyle,
                alignment = text.alignment,
                color = text.color,
                lineSpacing = text.lineSpacing,
                supportRichText = text.supportRichText,
                horizontalOverflow = text.horizontalOverflow,
                verticalOverflow = text.verticalOverflow,
                resizeTextForBestFit = text.resizeTextForBestFit,
                resizeTextMinSize = text.resizeTextMinSize,
                resizeTextMaxSize = text.resizeTextMaxSize,
                raycastTarget = text.raycastTarget,
                maskable = text.maskable
            };
        }

        /// <summary>
        /// Applies captured properties to a TextMeshProUGUI component.
        /// </summary>
        private static void ApplyPropertiesToTMP(TextMeshProUGUI tmp, TextProperties props, TMP_FontAsset tmpFont)
        {
            // Direct mappings
            tmp.text = props.text;
            tmp.font = tmpFont;
            tmp.fontSize = props.fontSize;
            tmp.color = props.color;
            tmp.lineSpacing = props.lineSpacing;
            tmp.richText = props.supportRichText;
            tmp.raycastTarget = props.raycastTarget;
            tmp.maskable = props.maskable;

            // Converted mappings
            tmp.fontStyle = ConvertFontStyle(props.fontStyle);
            tmp.alignment = ConvertAlignment(props.alignment);
            
            // Auto-sizing
            tmp.enableAutoSizing = props.resizeTextForBestFit;
            tmp.fontSizeMin = props.resizeTextMinSize;
            tmp.fontSizeMax = props.resizeTextMaxSize;

            // Overflow handling
            tmp.textWrappingMode = props.horizontalOverflow == HorizontalWrapMode.Wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = props.verticalOverflow == VerticalWrapMode.Truncate 
                ? TextOverflowModes.Truncate 
                : TextOverflowModes.Overflow;
        }

        /// <summary>
        /// Searches the project for a TMP_FontAsset matching "{FontName} SDF".
        /// </summary>
        private static TMP_FontAsset FindTMPFontAsset(Font legacyFont)
        {
            if (legacyFont == null)
            {
                // Try to find default TMP font
                return null;
            }

            string searchName = $"{legacyFont.name} SDF";
            
            // Search for all TMP_FontAsset in the project
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (fontAsset != null && fontAsset.name == searchName)
                {
                    return fontAsset;
                }
            }

            // Also try exact name match (without SDF) as fallback search
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (fontAsset != null && fontAsset.name == legacyFont.name)
                {
                    return fontAsset;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a disabled duplicate of the GameObject with "_Legacy" suffix.
        /// </summary>
        private static GameObject DuplicateAsLegacy(GameObject original)
        {
            var duplicate = Object.Instantiate(original, original.transform.parent);
            duplicate.name = $"{original.name}_Legacy";
            duplicate.SetActive(false);
            
            // Ensure duplicate is positioned right after original in hierarchy
            duplicate.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);
            
            return duplicate;
        }

        /// <summary>
        /// Converts legacy TextAnchor to TMP TextAlignmentOptions.
        /// </summary>
        private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.TopLeft
            };
        }

        /// <summary>
        /// Converts legacy FontStyle to TMP FontStyles.
        /// </summary>
        private static FontStyles ConvertFontStyle(FontStyle style)
        {
            return style switch
            {
                FontStyle.Normal => FontStyles.Normal,
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        /// <summary>
        /// Stores all Text component properties for migration.
        /// </summary>
        private struct TextProperties
        {
            public string text;
            public Font font;
            public int fontSize;
            public FontStyle fontStyle;
            public TextAnchor alignment;
            public Color color;
            public float lineSpacing;
            public bool supportRichText;
            public HorizontalWrapMode horizontalOverflow;
            public VerticalWrapMode verticalOverflow;
            public bool resizeTextForBestFit;
            public int resizeTextMinSize;
            public int resizeTextMaxSize;
            public bool raycastTarget;
            public bool maskable;
        }
    }
}
