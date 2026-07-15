using System;
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// CONTEXT menu on stock <see cref="Button"/>: migrate to <see cref="ButtonX"/>.
	/// Must remove Button before adding ButtonX (both inherit Selectable — Unity rejects dual Selectables).
	/// </summary>
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
				DebugX.Builder(LogChannels.Editor).WithContext(go).Warning(
					"Upgrade To ButtonX: No Image found on Button GameObject. Added Image component.");
			}

			// Capture Selectable/Button state before destroy — cannot AddComponent<ButtonX> while Button exists.
			bool interactable = sourceButton.interactable;
			var transition = sourceButton.transition;
			var colors = sourceButton.colors;
			var spriteState = sourceButton.spriteState;
			var navigation = sourceButton.navigation;
			var onClickSnapshot = CapturePersistentListeners(sourceButton.onClick);

			Undo.DestroyObjectImmediate(sourceButton);
			DebugX.Builder(LogChannels.Editor).WithContext(go).Info(
				"Upgrade To ButtonX: Removed original UnityEngine.UI.Button component.");

			var buttonX = Undo.AddComponent<ButtonX>(go);
			if (!buttonX)
			{
				DebugX.Builder(LogChannels.Editor).WithContext(go).Error(
					"Upgrade To ButtonX: Failed to add ButtonX component.");
				return;
			}

			buttonX.Interactable = interactable;
			buttonX.navigation = navigation;

			switch (transition)
			{
				case Selectable.Transition.ColorTint:
					buttonX.visualTransition = ButtonX.TransitionType.ColorTint;
					buttonX.normalColor = colors.normalColor;
					buttonX.highlightColor = colors.highlightedColor;
					buttonX.pressedColor = colors.pressedColor;
					buttonX.disabledColor = colors.disabledColor;
					buttonX.selectedColor = colors.highlightedColor;
					break;
				case Selectable.Transition.SpriteSwap:
					buttonX.visualTransition = ButtonX.TransitionType.SpriteSwap;
					buttonX.normalSprite = image ? image.sprite : null;
					buttonX.highlightSprite = spriteState.highlightedSprite ? spriteState.highlightedSprite : buttonX.normalSprite;
					buttonX.pressedSprite = spriteState.pressedSprite ? spriteState.pressedSprite : buttonX.normalSprite;
					buttonX.disabledSprite = spriteState.disabledSprite ? spriteState.disabledSprite : buttonX.normalSprite;
					buttonX.selectedSprite = spriteState.selectedSprite ? spriteState.selectedSprite : buttonX.normalSprite;
					break;
				default:
					buttonX.visualTransition = ButtonX.TransitionType.ColorTint;
					buttonX.normalColor = image ? image.color : Color.white;
					buttonX.highlightColor = buttonX.normalColor;
					buttonX.pressedColor = buttonX.normalColor;
					buttonX.disabledColor = new Color(
						buttonX.normalColor.r, buttonX.normalColor.g, buttonX.normalColor.b,
						buttonX.normalColor.a * 0.5f);
					buttonX.selectedColor = buttonX.highlightColor;
					break;
			}

			buttonX.image = image;

			TextMeshProUGUI tmp = null;
			string firstTextValue = null;
			Text legacyFound = null;
			var queue = new Queue<Transform>();
			queue.Enqueue(go.transform);

			while (queue.Count > 0 && tmp == null && legacyFound == null)
			{
				var t = queue.Dequeue();
				if (t != go.transform)
				{
					tmp = t.GetComponent<TextMeshProUGUI>();
					if (tmp)
					{
						firstTextValue = GetTmpText(tmp);
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
					queue.Enqueue(t.GetChild(i));
			}

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
				tmp.alignment = ConvertAlignment(alignment);
				firstTextValue = textValue;
				DebugX.Builder(LogChannels.Editor).WithContext(legacyGO).Info(
					"Upgrade To ButtonX: Converted legacy Text to TextMeshProUGUI.");
			}

			if (tmp)
			{
				buttonX.textMesh = tmp;
				buttonX.textMesh.text = firstTextValue;
			}

			if (!string.IsNullOrEmpty(firstTextValue))
				buttonX.Text = firstTextValue;
			else
				DebugX.Builder(LogChannels.Editor).WithContext(go).Warning(
					"Upgrade To ButtonX: No non-empty text value found to assign.");

			int copiedListeners = 0;
			foreach (var (target, method) in onClickSnapshot)
			{
				var action = Delegate.CreateDelegate(typeof(UnityAction), target, method, false) as UnityAction;
				if (action != null)
				{
					UnityEventTools.AddPersistentListener(buttonX.OnClicked, action);
					copiedListeners++;
				}
				else
				{
					DebugX.Builder(LogChannels.Editor).WithContext(go).Warning(
						"Upgrade To ButtonX: Failed to create UnityAction for persistent listener ({Target}.{Method}).",
						target, method);
				}
			}

			if (onClickSnapshot.Count == 0)
			{
				DebugX.Builder(LogChannels.Editor).WithContext(go).Info(
					"Upgrade To ButtonX: Source Button had no persistent onClick listeners.");
			}
			else
			{
				DebugX.Builder(LogChannels.Editor).WithContext(go).Info(
					"Upgrade To ButtonX: Copied {CopiedListeners}/{TotalListeners} onClick listeners.",
					copiedListeners, onClickSnapshot.Count);
			}

			EditorUtility.SetDirty(buttonX);
			EditorGUIUtility.PingObject(go);
			Selection.activeGameObject = go;
			DebugX.Builder(LogChannels.Editor).WithContext(go).Info("Upgrade To ButtonX: Upgrade complete.");
		}

		[MenuItem("CONTEXT/Button/Upgrade To ButtonX", true)]
		private static bool UpgradeValidate(MenuCommand command)
		{
			return command != null && command.context is Button button && button.GetComponent<ButtonX>() == null;
		}

		private static List<(UnityEngine.Object target, string method)> CapturePersistentListeners(UnityEventBase unityEvent)
		{
			var list = new List<(UnityEngine.Object, string)>();
			int count = unityEvent.GetPersistentEventCount();
			for (int i = 0; i < count; i++)
			{
				var target = unityEvent.GetPersistentTarget(i);
				var method = unityEvent.GetPersistentMethodName(i);
				if (target != null && !string.IsNullOrEmpty(method))
					list.Add((target, method));
			}
			return list;
		}

		private static string GetTmpText(TextMeshProUGUI c)
		{
			if (c == null) return null;
			try
			{
				var so = new SerializedObject(c);
				var sp = so.FindProperty("m_text");
				var val = sp != null ? sp.stringValue : null;
				if (!string.IsNullOrEmpty(val)) return val;
			}
			catch
			{
				// ignore serialized read failures; fall through
			}

			if (!string.IsNullOrEmpty(c.text)) return c.text;

			var mi = typeof(TMP_Text).GetMethod(
				"GetParsedText",
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic);
			if (mi != null)
			{
				try
				{
					var parsed = mi.Invoke(c, null) as string;
					if (!string.IsNullOrEmpty(parsed)) return parsed;
				}
				catch
				{
					// ignore
				}
			}

			return null;
		}

		private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
		{
			return alignment switch
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
				_ => TextAlignmentOptions.Center
			};
		}
	}
}
