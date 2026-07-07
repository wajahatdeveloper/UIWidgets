using System;
using System.Collections.Generic;
using FoundationPlatform.DebugX;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIWidgets.Editor
{
	public static class UpgradeToButtonX
	{
		[MenuItem("CONTEXT/Button/Upgrade To ButtonX")] 
		private static void Upgrade(MenuCommand command)
		{
			var sourceButton = command.context as Button;
		if (!sourceButton)
		{
			DebugX.Logger(LogChannels.Editor).Error("[UI:ERROR:Editor] UpgradeToButtonX: context is not UnityEngine.UI.Button.");
			return;
		}

			var go = sourceButton.gameObject;
			Undo.RegisterFullObjectHierarchyUndo(go, "Upgrade To ButtonX");

			var image = go.GetComponent<Image>();
		if (!image)
		{
			image = Undo.AddComponent<Image>(go);
			DebugX.Builder(LogChannels.Editor).WithContext(go).Warning("Upgrade To ButtonX: No Image found on Button GameObject. Added Image component.");
		}

			// Add ButtonX
			var buttonX = Undo.AddComponent<ButtonX>(go);
		if (!buttonX)
		{
			DebugX.Builder(LogChannels.Editor).WithContext(go).Error("Upgrade To ButtonX: Failed to add ButtonX component.");
			return;
		}

			// Migrate interactable
			buttonX.Interactable = sourceButton.interactable;

			// Migrate transition
			switch (sourceButton.transition)
			{
				case Selectable.Transition.ColorTint:
					buttonX.visualTransition = ButtonX.TransitionType.ColorTint;
					var cb = sourceButton.colors;
					buttonX.normalColor = cb.normalColor;
					buttonX.highlightColor = cb.highlightedColor;
					buttonX.pressedColor = cb.pressedColor;
					buttonX.disabledColor = cb.disabledColor;
					// Use highlighted as selected fallback
					buttonX.selectedColor = cb.highlightedColor;
					break;
				case Selectable.Transition.SpriteSwap:
					buttonX.visualTransition = ButtonX.TransitionType.SpriteSwap;
					var ss = sourceButton.spriteState;
					buttonX.normalSprite = image ? image.sprite : null;
					buttonX.highlightSprite = ss.highlightedSprite ? ss.highlightedSprite : buttonX.normalSprite;
					buttonX.pressedSprite = ss.pressedSprite ? ss.pressedSprite : buttonX.normalSprite;
					buttonX.disabledSprite = ss.disabledSprite ? ss.disabledSprite : buttonX.normalSprite;
					buttonX.selectedSprite = ss.selectedSprite ? ss.selectedSprite : buttonX.normalSprite;
					break;
				default:
					// Animation and None -> default to ColorTint with current image color
					buttonX.visualTransition = ButtonX.TransitionType.ColorTint;
					buttonX.normalColor = image ? image.color : Color.white;
					buttonX.highlightColor = buttonX.normalColor;
					buttonX.pressedColor = buttonX.normalColor;
					buttonX.disabledColor = new Color(buttonX.normalColor.r, buttonX.normalColor.g, buttonX.normalColor.b, buttonX.normalColor.a * 0.5f);
					buttonX.selectedColor = buttonX.highlightColor;
					break;
			}

			// Assign image reference
			buttonX.image = image;


			// Find first text component in hierarchy order (TMP or legacy), migrate its value into ButtonX
			TextMeshProUGUI tmp = null;
			string firstTextValue = null;
			Text legacyFound = null;
			var queue = new Queue<Transform>();
			queue.Enqueue(go.transform);
			// Helper to extract TMP text reliably in edit mode
			string _GetTmpText(TextMeshProUGUI c)
			{
				if (c == null) { return null; }
				try
				{
					var so = new SerializedObject(c);
					var sp = so.FindProperty("m_text");
					var val = sp != null ? sp.stringValue : null;
					if (!string.IsNullOrEmpty(val)) { return val; }
				}
				catch { }
				// Fallbacks
				if (!string.IsNullOrEmpty(c.text)) { return c.text; }
				// Optional: GetParsedText via reflection if present
				var mi = typeof(TMP_Text).GetMethod("GetParsedText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				if (mi != null)
				{
					try
					{
						var parsed = mi.Invoke(c, null) as string;
						if (!string.IsNullOrEmpty(parsed)) { return parsed; }
					}
					catch { }
				}
				return null;
			}

			while (queue.Count > 0 && tmp == null && legacyFound == null)
			{
				var t = queue.Dequeue();
				if (t != go.transform)
				{
					tmp = t.GetComponent<TextMeshProUGUI>();
					if (tmp)
					{
						firstTextValue = _GetTmpText(tmp);
						break;
					}
					legacyFound = t.GetComponent<Text>();
					if (legacyFound)
					{
						firstTextValue = legacyFound.text;
						break;
					}
				}
				for (int i = 0; i < t.childCount; i++)
				{
					queue.Enqueue(t.GetChild(i));
				}
			}

			// If legacy Text found first, convert it on the same GO
			if (tmp == null && legacyFound)
			{
				var legacyGO = legacyFound.gameObject;
				string textValue = legacyFound.text;
				Color textColor = legacyFound.color;
				int fontSize = Mathf.RoundToInt(legacyFound.fontSize);
				var alignment = legacyFound.alignment;

				Undo.DestroyObjectImmediate(legacyFound);
				tmp = Undo.AddComponent<TextMeshProUGUI>(legacyGO);
				tmp.text = textValue;
				tmp.color = textColor;
				tmp.fontSize = fontSize;
				switch (alignment)
				{
					case TextAnchor.UpperLeft: tmp.alignment = TextAlignmentOptions.TopLeft; break;
					case TextAnchor.UpperCenter: tmp.alignment = TextAlignmentOptions.Top; break;
					case TextAnchor.UpperRight: tmp.alignment = TextAlignmentOptions.TopRight; break;
					case TextAnchor.MiddleLeft: tmp.alignment = TextAlignmentOptions.Left; break;
					case TextAnchor.MiddleCenter: tmp.alignment = TextAlignmentOptions.Center; break;
					case TextAnchor.MiddleRight: tmp.alignment = TextAlignmentOptions.Right; break;
					case TextAnchor.LowerLeft: tmp.alignment = TextAlignmentOptions.BottomLeft; break;
					case TextAnchor.LowerCenter: tmp.alignment = TextAlignmentOptions.Bottom; break;
					case TextAnchor.LowerRight: tmp.alignment = TextAlignmentOptions.BottomRight; break;
				}
			firstTextValue = textValue;
			DebugX.Builder(LogChannels.Editor).WithContext(legacyGO).Info("Upgrade To ButtonX: Converted legacy Text to TextMeshProUGUI.");
		}

			// Assign resulting TMP to ButtonX and set the serialized Text field
			if (tmp)
			{
				buttonX.textMesh = tmp;
				buttonX.textMesh.text = firstTextValue;
			}
			if (!string.IsNullOrEmpty(firstTextValue))
			{
				buttonX.Text = firstTextValue;
			}
		else
		{
			DebugX.Builder(LogChannels.Editor).WithContext(go).Warning("Upgrade To ButtonX: No non-empty text value found to assign.");
		}

			// Copy onClick -> OnClicked via UnityEventTools using delegates
			int copiedListeners = 0;
			for (int i = 0; i < sourceButton.onClick.GetPersistentEventCount(); i++)
			{
				var target = sourceButton.onClick.GetPersistentTarget(i);
				var method = sourceButton.onClick.GetPersistentMethodName(i);
				if (target != null && !string.IsNullOrEmpty(method))
				{
					var action = Delegate.CreateDelegate(typeof(UnityAction), target, method, false) as UnityAction;
					if (action != null)
					{
						UnityEventTools.AddPersistentListener(buttonX.OnClicked, action);
						copiedListeners++;
					}
				else
				{
					DebugX.Builder(LogChannels.Editor).WithContext(go).Warning("Upgrade To ButtonX: Failed to create UnityAction for persistent listener {Index} ({Target}.{Method}).", i, target, method);
				}
				}
			}
		if (sourceButton.onClick.GetPersistentEventCount() == 0)
		{
			DebugX.Builder(LogChannels.Editor).WithContext(go).Info("Upgrade To ButtonX: Source Button had no persistent onClick listeners.");
		}
		else
		{
			DebugX.Builder(LogChannels.Editor).WithContext(go).Info("Upgrade To ButtonX: Copied {CopiedListeners}/{TotalListeners} onClick listeners.", copiedListeners, sourceButton.onClick.GetPersistentEventCount());
		}
			EditorUtility.SetDirty(buttonX);

		// Remove original Button
		Undo.DestroyObjectImmediate(sourceButton);
		DebugX.Builder(LogChannels.Editor).WithContext(go).Info("Upgrade To ButtonX: Removed original UnityEngine.UI.Button component.");

		EditorGUIUtility.PingObject(go);
		Selection.activeGameObject = go;
		DebugX.Builder(LogChannels.Editor).WithContext(go).Info("Upgrade To ButtonX: Upgrade complete.");
		}

		[MenuItem("CONTEXT/Button/Upgrade To ButtonX", true)]
		private static bool UpgradeValidate(MenuCommand command)
		{
			return command != null && command.context is Button;
		}
	}
}


