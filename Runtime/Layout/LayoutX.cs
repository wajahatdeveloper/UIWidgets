using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	/// <summary>
	/// Single layout component covering flow ("compact") and uniform-cell ("grid") arrangements.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("UI (Canvas)/Layout X")]
	public class LayoutX : LayoutGroup
	{
		public enum LayoutMode
		{
			/// <summary>Flow layout: elements keep their own sizes, lines wrap when full.</summary>
			Compact = 0,

			/// <summary>Uniform cells sized to the largest element.</summary>
			Grid = 1,
		}

		/// <summary>Main flow axis. Vertical = flow down, wrap to next column.</summary>
		public enum MainAxis
		{
			Horizontal = 0,
			Vertical = 1,
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
			/// <summary>Leave child RectTransform sizes alone; measure with rect size.</summary>
			None = 0,

			/// <summary>Drive child size to its preferred size.</summary>
			Preferred = 1,
		}

		[SerializeField] LayoutMode mode = LayoutMode.Compact;

		[FormerlySerializedAs("vertical")]
		[SerializeField] MainAxis mainAxis = MainAxis.Horizontal;

		[SerializeField] LineConstraint constraint = LineConstraint.Flexible;
		[SerializeField] int constraintCount = 1;
		[SerializeField] Vector2 spacing = new Vector2(5f, 5f);
		[SerializeField] LineAlignment lineAlignment = LineAlignment.Start;
		[SerializeField] LineAlignment crossAlignment = LineAlignment.Start;
		[SerializeField] SizeControl childWidth = SizeControl.None;
		[SerializeField] SizeControl childHeight = SizeControl.None;

		[Tooltip("Measure children by their RectTransform size instead of preferred layout size.")]
		[SerializeField] bool useChildRectSize;

		[Tooltip("Zero out child local rotation while laying out.")]
		[SerializeField] bool resetChildRotation;

		[Tooltip("Lay out children in reverse hierarchy order.")]
		[SerializeField] bool reverseArrangement;

		[Tooltip("Distribute free space along the main axis within each line.")]
		[SerializeField] bool childForceExpandMain;

		[Tooltip("Stretch children to the line thickness along the cross axis.")]
		[SerializeField] bool childForceExpandCross;

		public LayoutMode Mode { get => mode; set { mode = value; SetDirty(); } }

		public MainAxis Axis
		{
			get => mainAxis;
			set { mainAxis = value; SetDirty(); }
		}

		public LineConstraint Constraint { get => constraint; set { constraint = value; SetDirty(); } }
		public int ConstraintCount { get => Mathf.Max(1, constraintCount); set { constraintCount = value; SetDirty(); } }
		public Vector2 Spacing { get => spacing; set { spacing = value; SetDirty(); } }
		public LineAlignment LineAlign { get => lineAlignment; set { lineAlignment = value; SetDirty(); } }
		public LineAlignment CrossAlign { get => crossAlignment; set { crossAlignment = value; SetDirty(); } }
		public SizeControl ChildWidth { get => childWidth; set { childWidth = value; SetDirty(); } }
		public SizeControl ChildHeight { get => childHeight; set { childHeight = value; SetDirty(); } }
		public bool UseChildRectSize { get => useChildRectSize; set { useChildRectSize = value; SetDirty(); } }
		public bool ResetChildRotation { get => resetChildRotation; set { resetChildRotation = value; SetDirty(); } }
		public bool ReverseArrangement { get => reverseArrangement; set { reverseArrangement = value; SetDirty(); } }
		public bool ChildForceExpandMain { get => childForceExpandMain; set { childForceExpandMain = value; SetDirty(); } }
		public bool ChildForceExpandCross { get => childForceExpandCross; set { childForceExpandCross = value; SetDirty(); } }

		struct Entry
		{
			public RectTransform Rect;
			public Vector2 Measured; // natural measured size
			public Vector2 Slot; // layout slot (cell or expanded)
			public Vector2 Driven; // size written by Apply when driving
			public Vector2 Position; // child top-left relative to content block
		}

		struct Line
		{
			public int Start;
			public int Count;
			public float Main;
			public float Cross;
		}

		readonly List<Entry> entries = new List<Entry>();
		readonly List<Line> lines = new List<Line>();
		Vector2 contentSize;

		int MainAxisIndex => mainAxis == MainAxis.Vertical ? 1 : 0;
		int CrossAxisIndex => 1 - MainAxisIndex;
		float MainSpacing => mainAxis == MainAxis.Vertical ? spacing.y : spacing.x;
		float CrossSpacing => mainAxis == MainAxis.Vertical ? spacing.x : spacing.y;

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			MeasureChildren();

			if (mainAxis == MainAxis.Horizontal)
			{
				BuildLines(unconstrainedMain: true);
			}
			else
			{
				BuildLines(unconstrainedMain: false);
			}

			var width = contentSize.x + padding.horizontal;
			var min = MinChildAlong(0) + padding.horizontal;
			SetLayoutInputForAxis(min, width, -1f, 0);
		}

		public override void CalculateLayoutInputVertical()
		{
			MeasureChildren();

			if (mainAxis == MainAxis.Horizontal)
			{
				BuildLines(unconstrainedMain: false);
			}
			else
			{
				BuildLines(unconstrainedMain: true);
			}

			var height = contentSize.y + padding.vertical;
			var min = MinChildAlong(1) + padding.vertical;
			SetLayoutInputForAxis(min, height, -1f, 1);
		}

		public override void SetLayoutHorizontal()
		{
			BuildPlan();
			Apply(0);
			Apply(1);
		}

		public override void SetLayoutVertical()
		{
			// Rebuild: vertical-main wrap needs final height; re-apply both axes so positions stay in sync.
			BuildPlan();
			Apply(0);
			Apply(1);
		}

		void BuildPlan()
		{
			MeasureChildren();
			BuildLines(unconstrainedMain: false);
			ApplyExpand();
			PositionEntries();
		}

		float MinChildAlong(int axis)
		{
			var min = 0f;
			for (int i = 0; i < entries.Count; i++)
			{
				min = Mathf.Max(min, entries[i].Measured[axis]);
			}

			return min;
		}

		void MeasureChildren()
		{
			entries.Clear();
			var count = rectChildren.Count;
			for (int i = 0; i < count; i++)
			{
				var sourceIndex = reverseArrangement ? count - 1 - i : i;
				var child = rectChildren[sourceIndex];
				var measured = new Vector2(MeasureAxis(child, 0), MeasureAxis(child, 1));
				entries.Add(new Entry
				{
					Rect = child,
					Measured = measured,
					Slot = measured,
					Driven = measured,
				});
			}
		}

		float MeasureAxis(RectTransform child, int axis)
		{
			if (useChildRectSize)
			{
				return child.rect.size[axis];
			}

			var controlled = axis == 0 ? childWidth : childHeight;
			if (controlled == SizeControl.Preferred)
			{
				return LayoutUtility.GetPreferredSize(child, axis);
			}

			return child.rect.size[axis];
		}

		float AvailableMainLength()
		{
			var pad = MainAxisIndex == 0 ? padding.horizontal : padding.vertical;
			return rectTransform.rect.size[MainAxisIndex] - pad;
		}

		float AvailableCrossLength()
		{
			var pad = CrossAxisIndex == 0 ? padding.horizontal : padding.vertical;
			return rectTransform.rect.size[CrossAxisIndex] - pad;
		}

		void BuildLines(bool unconstrainedMain)
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
				BuildGridLines(perLineCap, unconstrainedMain);
			}
			else
			{
				BuildCompactLines(perLineCap, unconstrainedMain);
			}

			RecalcContentSize();
		}

		void BuildCompactLines(int perLineCap, bool unconstrainedMain)
		{
			var available = unconstrainedMain ? float.PositiveInfinity : AvailableMainLength();
			if (!unconstrainedMain && available <= 0f)
			{
				available = float.PositiveInfinity;
			}

			var lineStart = 0;
			var lineMain = 0f;
			var lineCross = 0f;

			for (int i = 0; i < entries.Count; i++)
			{
				var size = entries[i].Measured;
				var main = size[MainAxisIndex];
				var count = i - lineStart;
				var candidate = count == 0 ? main : lineMain + MainSpacing + main;
				var overflow = count > 0 && (count >= perLineCap || candidate > available);

				if (overflow)
				{
					lines.Add(new Line { Start = lineStart, Count = count, Main = lineMain, Cross = lineCross });
					lineStart = i;
					lineMain = main;
					lineCross = size[CrossAxisIndex];
				}
				else
				{
					lineMain = candidate;
					lineCross = Mathf.Max(lineCross, size[CrossAxisIndex]);
				}

				var entry = entries[i];
				entry.Slot = size;
				entries[i] = entry;
			}

			lines.Add(new Line { Start = lineStart, Count = entries.Count - lineStart, Main = lineMain, Cross = lineCross });
		}

		Vector2 GridCellSize()
		{
			var cell = Vector2.zero;
			for (int i = 0; i < entries.Count; i++)
			{
				cell = Vector2.Max(cell, entries[i].Measured);
			}

			return cell;
		}

		void BuildGridLines(int perLineCap, bool unconstrainedMain)
		{
			var cell = GridCellSize();
			var cellMain = cell[MainAxisIndex];
			var itemsPerLine = perLineCap;

			if (constraint == LineConstraint.Flexible)
			{
				if (unconstrainedMain)
				{
					itemsPerLine = entries.Count;
				}
				else
				{
					var available = AvailableMainLength();
					if (available <= 0f || cellMain + MainSpacing <= 0f)
					{
						itemsPerLine = entries.Count;
					}
					else
					{
						itemsPerLine = Mathf.Max(1, Mathf.FloorToInt((available + MainSpacing) / (cellMain + MainSpacing)));
					}
				}
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
					Cross = cell[CrossAxisIndex],
				});
			}

			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				entry.Slot = cell;
				entries[i] = entry;
			}
		}

		void ApplyExpand()
		{
			if (entries.Count == 0 || lines.Count == 0)
			{
				return;
			}

			var availableMain = AvailableMainLength();
			var availableCross = AvailableCrossLength();

			if (mode == LayoutMode.Grid && constraint == LineConstraint.Flexible &&
			    (childForceExpandMain || childForceExpandCross))
			{
				ApplyGridFlexibleExpand(availableMain, availableCross);
				return;
			}

			if (!childForceExpandMain && !childForceExpandCross)
			{
				return;
			}

			var totalCross = 0f;
			for (int i = 0; i < lines.Count; i++)
			{
				totalCross += lines[i].Cross;
			}

			totalCross += CrossSpacing * Mathf.Max(0, lines.Count - 1);

			var crossExtraPerLine = 0f;
			if (childForceExpandCross && availableCross > totalCross && lines.Count > 0)
			{
				crossExtraPerLine = (availableCross - totalCross) / lines.Count;
			}

			for (int l = 0; l < lines.Count; l++)
			{
				var line = lines[l];
				var lineCross = line.Cross + crossExtraPerLine;

				var mainExtraPerItem = 0f;
				if (childForceExpandMain && availableMain > line.Main && line.Count > 0)
				{
					mainExtraPerItem = (availableMain - line.Main) / line.Count;
				}

				for (int i = 0; i < line.Count; i++)
				{
					var index = line.Start + i;
					var entry = entries[index];
					var slot = entry.Slot;

					if (childForceExpandMain)
					{
						slot[MainAxisIndex] = entry.Measured[MainAxisIndex] + mainExtraPerItem;
					}

					if (childForceExpandCross)
					{
						slot[CrossAxisIndex] = lineCross;
					}

					entry.Slot = slot;
					entries[index] = entry;
				}

				if (childForceExpandMain && availableMain > line.Main)
				{
					line.Main = availableMain;
				}

				line.Cross = lineCross;
				lines[l] = line;
			}

			RecalcContentSize();
		}

		void ApplyGridFlexibleExpand(float availableMain, float availableCross)
		{
			var itemsPerLine = lines[0].Count;
			var cell = entries[0].Slot;

			if (childForceExpandMain && availableMain > 0f && itemsPerLine > 0)
			{
				var spacingTotal = MainSpacing * Mathf.Max(0, itemsPerLine - 1);
				cell[MainAxisIndex] = Mathf.Max(cell[MainAxisIndex], (availableMain - spacingTotal) / itemsPerLine);
			}

			if (childForceExpandCross && availableCross > 0f)
			{
				var spacingTotal = CrossSpacing * Mathf.Max(0, lines.Count - 1);
				cell[CrossAxisIndex] = Mathf.Max(cell[CrossAxisIndex], (availableCross - spacingTotal) / lines.Count);
			}

			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				entry.Slot = cell;
				entries[i] = entry;
			}

			for (int l = 0; l < lines.Count; l++)
			{
				var line = lines[l];
				line.Main = (cell[MainAxisIndex] * line.Count) + (MainSpacing * Mathf.Max(0, line.Count - 1));
				line.Cross = cell[CrossAxisIndex];
				lines[l] = line;
			}

			RecalcContentSize();
		}

		void RecalcContentSize()
		{
			var main = 0f;
			var cross = 0f;
			for (int i = 0; i < lines.Count; i++)
			{
				main = Mathf.Max(main, lines[i].Main);
				cross += lines[i].Cross;
			}

			cross += CrossSpacing * Mathf.Max(0, lines.Count - 1);
			contentSize = MainAxisIndex == 0 ? new Vector2(main, cross) : new Vector2(cross, main);
		}

		void PositionEntries()
		{
			var groupMain = contentSize[MainAxisIndex];
			var crossOffset = 0f;

			for (int l = 0; l < lines.Count; l++)
			{
				var line = lines[l];
				var mainOffset = (groupMain - line.Main) * AlignmentFactor(lineAlignment);

				for (int i = 0; i < line.Count; i++)
				{
					var index = line.Start + i;
					var entry = entries[index];
					var slotMain = entry.Slot[MainAxisIndex];

					var driven = entry.Measured;
					if (childForceExpandMain)
					{
						driven[MainAxisIndex] = slotMain;
					}

					if (childForceExpandCross)
					{
						driven[CrossAxisIndex] = entry.Slot[CrossAxisIndex];
					}

					// Within-slot alignment: main starts at slot origin; cross uses crossAlignment.
					var mainPos = mainOffset;
					var crossPos = crossOffset + ((line.Cross - driven[CrossAxisIndex]) * AlignmentFactor(crossAlignment));

					// Grid: align measured child inside uniform cell (cross uses crossAlignment; main stays Start).
					if (mode == LayoutMode.Grid && !childForceExpandCross)
					{
						crossPos = crossOffset + ((line.Cross - driven[CrossAxisIndex]) * AlignmentFactor(crossAlignment));
					}

					entry.Driven = driven;
					entry.Position = MainAxisIndex == 0
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

		bool DrivesSize(int axis)
		{
			if (useChildRectSize)
			{
				return (axis == MainAxisIndex && childForceExpandMain) ||
				       (axis == CrossAxisIndex && childForceExpandCross);
			}

			var controlled = axis == 0 ? childWidth : childHeight;
			if (controlled == SizeControl.Preferred)
			{
				return true;
			}

			return (axis == MainAxisIndex && childForceExpandMain) ||
			       (axis == CrossAxisIndex && childForceExpandCross);
		}

		void Apply(int axis)
		{
			var startOffset = GetStartOffset(axis, contentSize[axis]);

			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];

				if (resetChildRotation && axis == 0)
				{
					m_Tracker.Add(this, entry.Rect, DrivenTransformProperties.Rotation);
					entry.Rect.localRotation = Quaternion.identity;
				}

				if (DrivesSize(axis))
				{
					SetChildAlongAxis(entry.Rect, axis, startOffset + entry.Position[axis], entry.Driven[axis]);
				}
				else
				{
					SetChildAlongAxis(entry.Rect, axis, startOffset + entry.Position[axis]);
				}
			}
		}
	}
}
