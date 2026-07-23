#if UNITY_EDITOR
using System.Collections.Generic;
using AetherNexus.FoundationPlatform.AetherInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Custom inspector for <see cref="ButtonX"/>. Because ButtonX derives from
	/// <c>UnityEngine.UI.Selectable</c>, Unity's built-in SelectableEditor would otherwise hijack the
	/// inspector; this more-specific editor takes precedence. Hides inherited Selectable visual fields
	/// (ButtonX owns color/sprite state machine) and draws the rest via AetherInspector. Navigation
	/// is drawn flat at the end (no extra foldout wrapper).
	/// </summary>
	[CustomEditor(typeof(ButtonX))]
	[CanEditMultipleObjects]
	public sealed class ButtonXEditor : AetherInspectorEditor
	{
		// Inherited Selectable serialized fields that ButtonX supersedes with its own visual system.
		// m_Navigation is drawn flat after the engine pass (kept out of skip so we own placement).
		private static readonly HashSet<string> HiddenFields = new HashSet<string>
		{
			"m_TargetGraphic", "m_Transition", "m_Colors", "m_SpriteState",
			"m_AnimationTriggers", "m_Interactable", "m_Navigation",
		};

		private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _tabs = new Dictionary<string, int>();

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			AetherInspectorRenderer.Draw(this, serializedObject, target, _foldouts, _tabs,
				drawScriptRow: true, skipFields: HiddenFields);
			DrawNavigationFlat();
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawNavigationFlat()
		{
			var navigation = serializedObject.FindProperty("m_Navigation");
			if (navigation == null) { return; }

			EditorGUILayout.Space(4);
			EditorGUILayout.PropertyField(navigation, true);
		}
	}
}
#endif
