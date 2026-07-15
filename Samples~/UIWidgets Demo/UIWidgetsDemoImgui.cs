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
		public const float RowH = 20f;
		public const float Gap = 2f;
		public const float BlockGap = 8f;
		public const float ContentY = 48f;
		public const float BottomPad = 8f;
		public const float ScrollbarW = 14f;

		/// <summary>Rail width scales with screen; clamped for readability.</summary>
		public static float Width => Mathf.Clamp(Screen.width * 0.16f, 120f, 180f);

		private static Section _section = Section.Modals;
		private static Section _lastNotified = (Section)(-1);
		private static Vector2 _scroll;
		private static float _contentHeight = 1f;
		private static bool _inScroll;

		public static Section CurrentSection => _section;

		public static bool IsSection(Section section) => _section == section;

		/// <summary>True once per frame when the active section changed (call from OnGUI).</summary>
		public static bool ConsumeSectionChanged()
		{
			if (_section == _lastNotified)
				return false;
			_lastNotified = _section;
			_scroll = Vector2.zero;
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

		/// <summary>
		/// Opens a scroll view for harness controls. Returns content Y (0). Call <see cref="EndContent"/> after drawing.
		/// </summary>
		public static float BeginContent()
		{
			float top = ContentY;
			float viewH = Mathf.Max(48f, Screen.height - top - BottomPad);
			float viewW = Width + ScrollbarW + 4f;
			float contentH = Mathf.Max(viewH, _contentHeight);

			_scroll = GUI.BeginScrollView(
				new Rect(Left, top, viewW, viewH),
				_scroll,
				new Rect(0f, 0f, Width, contentH),
				false,
				true);
			_inScroll = true;
			return 0f;
		}

		/// <summary>Closes the harness scroll view. Pass the final content Y after all controls.</summary>
		public static void EndContent(float contentY)
		{
			_contentHeight = Mathf.Max(1f, contentY + Gap);
			if (_inScroll)
			{
				GUI.EndScrollView();
				_inScroll = false;
			}
		}

		/// <summary>Gap between harness blocks when drawn from a single OnGUI.</summary>
		public static void BlockSpacer(ref float y) => y += BlockGap;

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
			float x = _inScroll ? 0f : Left;
			bool hit = GUI.Button(new Rect(x, y, Width, RowH), label);
			y += RowH + Gap;
			return hit;
		}

		public static void Label(ref float y, string text)
		{
			float x = _inScroll ? 0f : Left;
			float h = RowH * 1.5f;
			GUI.Label(new Rect(x, y, Width, h), text);
			y += h + Gap;
		}
	}
}
#endif
