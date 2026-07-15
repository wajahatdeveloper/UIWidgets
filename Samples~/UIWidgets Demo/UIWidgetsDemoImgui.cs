#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	/// <summary>Shared narrow left-rail IMGUI layout for sample harness scripts.</summary>
	public static class UIWidgetsDemoImgui
	{
		public enum Section
		{
			Modals = 0,
			Buttons = 1,
			Lists = 2,
			Feedback = 3,
			Layout = 4,
		}

		public const float Left = 4f;
		public const float Width = 128f;
		public const float RowH = 18f;
		public const float Gap = 1f;
		public const float ContentY = 48f;

		private static Section _section = Section.Modals;
		private static Section _lastNotified = (Section)(-1);

		public static Section CurrentSection => _section;

		public static bool IsSection(Section section) => _section == section;

		/// <summary>True once per frame when the active section changed (call from OnGUI).</summary>
		public static bool ConsumeSectionChanged()
		{
			if (_section == _lastNotified)
				return false;
			_lastNotified = _section;
			return true;
		}

		/// <summary>Draw once from <see cref="DemoGalleryBootstrap"/> (or any single harness).</summary>
		public static void DrawSectionTabs()
		{
			float x = Left;
			float y = 4f;
			float w = 24f;
			DrawTab(ref x, y, w, "M", Section.Modals);
			DrawTab(ref x, y, w, "B", Section.Buttons);
			DrawTab(ref x, y, w, "L", Section.Lists);
			DrawTab(ref x, y, w, "F", Section.Feedback);
			DrawTab(ref x, y, w, "X", Section.Layout);
			GUI.Label(new Rect(Left, 26f, Width, RowH), SectionLabel(_section));
		}

		private static void DrawTab(ref float x, float y, float w, string label, Section section)
		{
			var prev = GUI.color;
			if (_section == section)
				GUI.color = Color.cyan;
			if (GUI.Button(new Rect(x, y, w, RowH), label))
				_section = section;
			GUI.color = prev;
			x += w + 2f;
		}

		private static string SectionLabel(Section section) => section switch
		{
			Section.Modals => "Modals",
			Section.Buttons => "Buttons",
			Section.Lists => "Lists",
			Section.Feedback => "Feedback",
			Section.Layout => "Layout",
			_ => section.ToString(),
		};

		public static bool Button(ref float y, string label)
		{
			bool hit = GUI.Button(new Rect(Left, y, Width, RowH), label);
			y += RowH + Gap;
			return hit;
		}

		public static void Label(ref float y, string text)
		{
			GUI.Label(new Rect(Left, y, Width, RowH * 1.5f), text);
			y += RowH * 1.5f + Gap;
		}
	}
}
#endif
