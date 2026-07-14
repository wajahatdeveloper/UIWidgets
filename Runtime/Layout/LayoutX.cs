using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Single layout component covering flow ("compact") and uniform-cell ("grid") arrangements
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("UI/UIWidgets/Layout X")]
	public class LayoutX : LayoutGroup
	{
		public enum LayoutMode
		{
			/// <summary>Flow layout: elements keep their own sizes, lines wrap when full.</summary>
			Compact = 0,

			/// <summary>Uniform cells sized to the largest element.</summary>
			Grid = 1,
		}

		/// <summary>Constraints are axis-relative: a "line" runs along the main axis.</summary>
		public enum LineConstraint
		{
			Flexible = 0,
			MaxItemsPerLine = 1,
			MaxLines = 2,
		}

		public enum LineAlignment
		{
			Start = 0,
			Center = 1,
			End = 2,
		}

		public enum SizeControl
		{
			/// <summary>Leave child RectTransform sizes alone.</summary>
			None = 0,

			/// <summary>Drive child size to its preferred size.</summary>
			Preferred = 1,
		}

		[SerializeField] LayoutMode mode = LayoutMode.Compact;
		[SerializeField] bool vertical;
		[SerializeField] LineConstraint constraint = LineConstraint.Flexible;
		[SerializeField] int constraintCount = 1;
		[SerializeField] Vector2 spacing = new Vector2(5f, 5f);
		[SerializeField] LineAlignment lineAlignment = LineAlignment.Start;
		[SerializeField] LineAlignment crossAlignment = LineAlignment.Start;
		[SerializeField] SizeControl childWidth = SizeControl.None;
		[SerializeField] SizeControl childHeight = SizeControl.None;

		[Tooltip("Measure children by their RectTransform size instead of their preferred layout size.")]
		[SerializeField] bool useChildRectSize;

		[Tooltip("Zero out child local rotation while laying out.")]
		[SerializeField] bool resetChildRotation;

		public LayoutMode Mode { get => mode; set { mode = value; SetDirty(); } }
		public bool Vertical { get => vertical; set { vertical = value; SetDirty(); } }
		public LineConstraint Constraint { get => constraint; set { constraint = value; SetDirty(); } }
		public int ConstraintCount { get => Mathf.Max(1, constraintCount); set { constraintCount = value; SetDirty(); } }
		public Vector2 Spacing { get => spacing; set { spacing = value; SetDirty(); } }
		public LineAlignment LineAlign { get => lineAlignment; set { lineAlignment = value; SetDirty(); } }
		public LineAlignment CrossAlign { get => crossAlignment; set { crossAlignment = value; SetDirty(); } }
		public SizeControl ChildWidth { get => childWidth; set { childWidth = value; SetDirty(); } }
		public SizeControl ChildHeight { get => childHeight; set { childHeight = value; SetDirty(); } }
		public bool UseChildRectSize { get => useChildRectSize; set { useChildRectSize = value; SetDirty(); } }
		public bool ResetChildRotation { get => resetChildRotation; set { resetChildRotation = value; SetDirty(); } }

		struct Entry
		{
			public RectTransform Rect;
			public Vector2 Size;
			public Vector2 CellSizeOverride; // grid mode: the uniform slot the entry sits in
			public Vector2 Position; // top-left corner relative to content block origin
		}

		struct Line
		{
			public int Start;
			public int Count;
			public float Main;  // occupied length along main axis
			public float Cross; // thickness along cross axis
		}

		readonly List<Entry> entries = new List<Entry>();
		readonly List<Line> lines = new List<Line>();
		Vector2 contentSize;

		int MainAxis => vertical ? 1 : 0;
		int CrossAxis => vertical ? 0 : 1;
		float MainSpacing => vertical ? spacing.y : spacing.x;
		float CrossSpacing => vertical ? spacing.x : spacing.y;

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			BuildPlan();
			var width = contentSize.x + padding.horizontal;
			SetLayoutInputForAxis(width, width, -1f, 0);
		}

		public override void CalculateLayoutInputVertical()
		{
			var height = contentSize.y + padding.vertical;
			SetLayoutInputForAxis(height, height, -1f, 1);
		}

		public override void SetLayoutHorizontal()
		{
			BuildPlan();
			Apply(0);
		}

		public override void SetLayoutVertical()
		{
			Apply(1);
		}

		void BuildPlan()
		{
			MeasureChildren();
			BuildLines();
			PositionEntries();
		}

		void MeasureChildren()
		{
			entries.Clear();
			for (int i = 0; i < rectChildren.Count; i++)
			{
				var child = rectChildren[i];
				entries.Add(new Entry
				{
					Rect = child,
					Size = new Vector2(MeasureAxis(child, 0), MeasureAxis(child, 1)),
				});
			}
		}

		float MeasureAxis(RectTransform child, int axis)
		{
			if (useChildRectSize)
			{
				return child.rect.size[axis];
			}

			var preferred = LayoutUtility.GetPreferredSize(child, axis);
			var controlled = axis == 0 ? childWidth : childHeight;
			return controlled == SizeControl.Preferred ? preferred : (preferred > 0f ? preferred : child.rect.size[axis]);
		}

		float AvailableMainLength()
		{
			var inner = rectTransform.rect.size[MainAxis] - (MainAxis == 0 ? padding.horizontal : padding.vertical);
			return Mathf.Max(0f, inner);
		}

		void BuildLines()
		{
			lines.Clear();
			if (entries.Count == 0)
			{
				contentSize = Vector2.zero;
				return;
			}

			int perLineCap = int.MaxValue;
			if (constraint == LineConstraint.MaxItemsPerLine)
			{
				perLineCap = ConstraintCount;
			}
			else if (constraint == LineConstraint.MaxLines)
			{
				perLineCap = Mathf.CeilToInt(entries.Count / (float)ConstraintCount);
			}

			if (mode == LayoutMode.Grid)
			{
				BuildGridLines(perLineCap);
			}
			else
			{
				BuildCompactLines(perLineCap);
			}

			var main = 0f;
			var cross = 0f;
			for (int i = 0; i < lines.Count; i++)
			{
				main = Mathf.Max(main, lines[i].Main);
				cross += lines[i].Cross;
			}

			cross += CrossSpacing * Mathf.Max(0, lines.Count - 1);
			contentSize = MainAxis == 0 ? new Vector2(main, cross) : new Vector2(cross, main);
		}

		void BuildCompactLines(int perLineCap)
		{
			var available = AvailableMainLength();
			var lineStart = 0;
			var lineMain = 0f;
			var lineCross = 0f;

			for (int i = 0; i < entries.Count; i++)
			{
				var size = entries[i].Size;
				var main = size[MainAxis];
				var count = i - lineStart;
				var candidate = count == 0 ? main : lineMain + MainSpacing + main;
				var overflow = count > 0 && (count >= perLineCap || candidate > available);

				if (overflow)
				{
					lines.Add(new Line { Start = lineStart, Count = count, Main = lineMain, Cross = lineCross });
					lineStart = i;
					lineMain = main;
					lineCross = size[CrossAxis];
				}
				else
				{
					lineMain = candidate;
					lineCross = Mathf.Max(lineCross, size[CrossAxis]);
				}
			}

			lines.Add(new Line { Start = lineStart, Count = entries.Count - lineStart, Main = lineMain, Cross = lineCross });
		}

		Vector2 GridCellSize()
		{
			var cell = Vector2.zero;
			for (int i = 0; i < entries.Count; i++)
			{
				cell = Vector2.Max(cell, entries[i].Size);
			}

			return cell;
		}

		void BuildGridLines(int perLineCap)
		{
			var cell = GridCellSize();
			var cellMain = cell[MainAxis];
			var itemsPerLine = perLineCap;

			if (constraint == LineConstraint.Flexible)
			{
				var available = AvailableMainLength();
				itemsPerLine = cellMain + MainSpacing > 0f
					? Mathf.FloorToInt((available + MainSpacing) / (cellMain + MainSpacing))
					: entries.Count;
			}

			itemsPerLine = Mathf.Clamp(itemsPerLine, 1, entries.Count);

			for (int start = 0; start < entries.Count; start += itemsPerLine)
			{
				var count = Mathf.Min(itemsPerLine, entries.Count - start);
				lines.Add(new Line
				{
					Start = start,
					Count = count,
					Main = (cellMain * count) + (MainSpacing * (count - 1)),
					Cross = cell[CrossAxis],
				});
			}

			// Uniform cells: every entry occupies a cell-sized slot.
			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				entry.CellSizeOverride = cell;
				entries[i] = entry;
			}
		}

		void PositionEntries()
		{
			var groupMain = contentSize[MainAxis];
			var crossOffset = 0f;

			for (int l = 0; l < lines.Count; l++)
			{
				var line = lines[l];
				var mainOffset = (groupMain - line.Main) * AlignmentFactor(lineAlignment);

				for (int i = 0; i < line.Count; i++)
				{
					var index = line.Start + i;
					var entry = entries[index];
					var slot = mode == LayoutMode.Grid ? entry.CellSizeOverride : entry.Size;
					var slotMain = slot[MainAxis];
					var slotCross = line.Cross;

					var mainPos = mainOffset;
					var crossPos = crossOffset + ((slotCross - entry.Size[CrossAxis]) * AlignmentFactor(crossAlignment));

					entry.Position = MainAxis == 0
						? new Vector2(mainPos, crossPos)
						: new Vector2(crossPos, mainPos);
					entries[index] = entry;

					mainOffset += slotMain + MainSpacing;
				}

				crossOffset += line.Cross + CrossSpacing;
			}
		}

		static float AlignmentFactor(LineAlignment alignment)
		{
			return alignment switch
			{
				LineAlignment.Center => 0.5f,
				LineAlignment.End => 1f,
				_ => 0f,
			};
		}

		void Apply(int axis)
		{
			var startOffset = GetStartOffset(axis, contentSize[axis]);

			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				var controlled = axis == 0 ? childWidth : childHeight;

				if (resetChildRotation && axis == 0)
				{
					m_Tracker.Add(this, entry.Rect, DrivenTransformProperties.Rotation);
					entry.Rect.localRotation = Quaternion.identity;
				}

				if (controlled == SizeControl.Preferred && !useChildRectSize)
				{
					SetChildAlongAxis(entry.Rect, axis, startOffset + entry.Position[axis], entry.Size[axis]);
				}
				else
				{
					SetChildAlongAxis(entry.Rect, axis, startOffset + entry.Position[axis]);
				}
			}
		}
	}
}
