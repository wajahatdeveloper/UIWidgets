#if UNITY_EDITOR
using System.Collections.Generic;
using FoundationPlatform.FrameworkInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace UIWidgets.Editor
{
	/// <summary>
	/// Custom inspector for <see cref="ButtonX"/>. Because ButtonX derives from
	/// <c>UnityEngine.UI.Selectable</c>, Unity's built-in SelectableEditor would otherwise hijack the
	/// inspector; this more-specific editor takes precedence. Replaces the former plugin editor +
	/// attribute-processor: it hides the inherited Selectable visual fields (ButtonX owns its own
	/// color/sprite state machine, so the base target-graphic / transition / color-block / sprite-state /
	/// animation-triggers / interactable fields are noise) and draws everything else natively.
	/// </summary>
	[CustomEditor(typeof(ButtonX))]
	[CanEditMultipleObjects]
	public sealed class ButtonXEditor : FrameworkEditor
	{
		private const string InfoTooltip =
			"Visuals are handled by ButtonX's own Colors / Sprites (below), which support extra tint " +
			"graphics, a Loading state, and a persistent toggle-Selected look. Selectable's nav, " +
			"interactable, and EventSystem integration are all active — see Navigation.";

		// Inherited Selectable serialized fields that ButtonX supersedes with its own visual system.
		// m_Navigation is drawn in DrawNavigationSection (Selectable-style foldout).
		private static readonly HashSet<string> HiddenFields = new HashSet<string>
		{
			"m_TargetGraphic", "m_Transition", "m_Colors", "m_SpriteState",
			"m_AnimationTriggers", "m_Interactable", "m_Navigation",
		};

		private const string NavigationFoldoutKey = "btnx:navigation";

		private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
		private readonly Dictionary<string, int> _tabs = new Dictionary<string, int>();

		public override void OnInspectorGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				var baseIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
				GUILayout.Label(new GUIContent(baseIcon != null ? baseIcon.image : null, InfoTooltip),
					GUILayout.Width(18), GUILayout.Height(18));
			}

			// Engine draw (honors ButtonX's [FoldoutGroup]/[InlineEditor]/[ShowInInspector]/[PropertyOrder])
			// while skipping the inherited Selectable visual fields ButtonX replaces.
			serializedObject.Update();
			FrameworkInspectorRenderer.Draw(this, serializedObject, target, _foldouts, _tabs,
				drawScriptRow: true, skipFields: HiddenFields);
			DrawNavigationSection();
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawNavigationSection()
		{
			var navigation = serializedObject.FindProperty("m_Navigation");
			if (navigation == null) { return; }

			EditorGUILayout.Space(4);
			if (!_foldouts.TryGetValue(NavigationFoldoutKey, out bool expanded)) { expanded = true; }
			expanded = EditorGUILayout.Foldout(expanded, "Navigation", true);
			_foldouts[NavigationFoldoutKey] = expanded;
			if (!expanded) { return; }

			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(navigation, true);
			EditorGUI.indentLevel--;
		}
	}
}
#endif
