using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UIEButton = UnityEngine.UIElements.Button;
using UIEImage = UnityEngine.UIElements.Image;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// UIElements view for the palette: an icon-tile grid grouped by category, a live search, a
	/// recents strip, and collapsible tool sections. Built once via <see cref="BuildPaletteRoot"/>
	/// and shared by the floating window (CreateGUI) and the Scene View overlay. All create logic
	/// lives in UIWidgetsWindow.UI.cs.
	///
	/// Tile gestures: left-click = create as Child (matches the native right-click-create flow),
	/// drag = drop into the Scene, right-click = Child / Sibling / Parent menu.
	/// </summary>
	public partial class UIWidgets
	{
		private const string PrefKey_Recents = "UIWidgets.Recents";
		private const string PrefKey_FoldoutPrefix = "UIWidgets.Foldout.";
		private const string ProjectUIPrefabsCategory = "Project UI Prefabs";
		private const int RecentsMax = 6;
		private const char RecentsDelim = '|';

		static UIWidgets()
		{
			ProjectUIPrefabScanCache.Invalidated += OnProjectUIPrefabsInvalidated;
		}

		private static void OnProjectUIPrefabsInvalidated()
		{
			var instances = Resources.FindObjectsOfTypeAll<UIWidgets>();
			if (instances == null) return;
			foreach (var window in instances)
			{
				if (window != null)
					window.RefreshPalette();
			}
		}

		private const float TileWidth = 84f;
		private const float TileHeight = 78f;
		private const float TileIconSize = 40f;

		private VisualElement _paletteRoot;
		private VisualElement _gridContainer;
		private VisualElement _recentsSection;
		private VisualElement _recentsStrip;

		private struct TileInfo
		{
			public string name;
			public string category;
			public GameObject prefab;      // set for spawn tiles
			public System.Type component;  // set for attach-component tiles
			public bool noCanvas;

			public bool IsComponent => component != null;
		}

		// Attach-component tiles, surfaced in the grid alongside widgets. Left-click attaches the
		// component to the selection (or a new Canvas child). Grouped under their own categories.
		private static readonly (string category, string name, System.Type type)[] ComponentTiles =
		{
			("Behaviours", "Safe Area", typeof(SafeArea)),
			("Behaviours", "UI Default State", typeof(UIDefaultState)),
			("Behaviours", "Auto UI Refs", typeof(AutoUIRefs)),
			("Effects", "Gradient", typeof(UIGradient)),
			("Effects", "Canvas Particles", typeof(UICanvasParticles)),
			("Effects", "Soft Mask", typeof(UISoftMask)),
			("Effects", "Raycast Alpha Mask", typeof(UIRaycastAlphaMask)),
			("Effects", "Flip", typeof(UIFlip)),
			("Effects", "Shine", typeof(UIShine)),
			("Effects", "Tele Type", typeof(TeleType)),
			("Primitives", "UI Corner Cut", typeof(UICornerCut)),
			("Primitives", "UI Polygon", typeof(UIPolygon)),
			("Primitives", "UI Primitive Base", typeof(UIPrimitiveBase)),
			("Primitives", "Diamond Graph", typeof(DiamondGraph)),
			("Primitives", "UI Circle", typeof(UICircle)),
			("Primitives", "UI Line Renderer", typeof(UILineRenderer)),
			("Primitives", "UI Squircle", typeof(UISquircle)),
			("Primitives", "UI Grid Renderer", typeof(UIGridRenderer)),
		};

		internal VisualElement BuildPaletteRoot()
		{
			_paletteRoot = new VisualElement { name = "UIWidgetsPalette" };
			_paletteRoot.style.flexGrow = 1;
			_paletteRoot.style.minWidth = 260;
			PopulatePalette();
			return _paletteRoot;
		}

		private void RefreshPalette()
		{
			if (_paletteRoot == null) return;
			_paletteRoot.Clear();
			PopulatePalette();
		}

		private void PopulatePalette()
		{
			EnsureAssetLoaded();

			if (uiWidgetsAsset == null)
			{
				_paletteRoot.Add(new HelpBox(
					"UI Widgets asset not found. Open a scene that references it, or create one via Assets → Create → UI Widgets.",
					HelpBoxMessageType.Info));
				_paletteRoot.Add(new UIEButton(() => { TryLoadAsset(); RefreshPalette(); }) { text = "Locate Asset" });
				return;
			}

			// Fixed header: options + search-with-Select always visible.
			_paletteRoot.Add(BuildOptionsBar());
			_paletteRoot.Add(BuildSearchRow());

			// Everything else scrolls together so expanding a category can never overlap other
			// content; the ScrollView takes the remaining bounded height.
			var scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.style.flexGrow = 1;
			scroll.style.flexShrink = 1;
			scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;

			_recentsSection = BuildRecentsSection();
			scroll.Add(_recentsSection);

			_gridContainer = new VisualElement();
			scroll.Add(_gridContainer);
			_paletteRoot.Add(scroll);

			RebuildGrid();
			RefreshRecents();
		}

		#region Options + search

		private VisualElement BuildOptionsBar()
		{
			var bar = new Toolbar();
			bar.Add(MakeToggle("Prefabs", "Instantiate as a prefab link (not a plain clone)", isInstantiatingPrefab,
				v => { isInstantiatingPrefab = v; EditorPrefs.SetBool(PrefKey_IsInstantiatingPrefab, v); }));
			bar.Add(MakeToggle("Auto-Select", "Select the new object after creation", autoSelectNewItems,
				v => { autoSelectNewItems = v; EditorPrefs.SetBool(PrefKey_AutoSelectNewItems, v); }));
			bar.Add(MakeToggle("Auto-Name", "Prompt to rename the object after creation", useAutoNaming,
				v => { useAutoNaming = v; EditorPrefs.SetBool(PrefKey_UseAutoNaming, v); }));
			bar.Add(MakeToggle("Prefer Canvas", "Reuse an existing Canvas instead of creating a new one", preferExistingCanvas,
				v => { preferExistingCanvas = v; EditorPrefs.SetBool(PrefKey_PreferExistingCanvas, v); }));
			return bar;
		}

		private static ToolbarToggle MakeToggle(string label, string tip, bool value, System.Action<bool> onChange)
		{
			var t = new ToolbarToggle { text = label, value = value, tooltip = tip };
			t.style.fontSize = 10;
			t.RegisterValueChangedCallback(e => onChange(e.newValue));
			return t;
		}

		private VisualElement BuildSearchRow()
		{
			var row = new Toolbar();
			row.style.marginTop = 2;
			row.style.marginBottom = 2;

			var search = new ToolbarSearchField { value = searchQuery ?? string.Empty };
			search.style.flexGrow = 1;
			search.RegisterValueChangedCallback(e =>
			{
				searchQuery = e.newValue;
				EditorPrefs.SetString(PrefKey_SearchQuery, searchQuery ?? string.Empty);
				RebuildGrid();
			});
			row.Add(search);

			var focus2D = new ToolbarButton(FocusSelectedUIIn2D)
			{
				text = "2D Focus",
				tooltip = "Enable Scene View 2D mode and frame the selected UI object"
			};
			row.Add(focus2D);

			var select = new ToolbarMenu { text = "Select", tooltip = "Bulk-select existing objects in the scene" };
			select.menu.AppendAction("All Button + Text", _ => SelectButtonsWith<Text>(false));
			select.menu.AppendAction("All Button + Text (under selection)", _ => SelectButtonsWith<Text>(true));
			select.menu.AppendAction("All Button + TMP", _ => SelectButtonsWith<TextMeshProUGUI>(false));
			select.menu.AppendAction("All Button + TMP (under selection)", _ => SelectButtonsWith<TextMeshProUGUI>(true));
			row.Add(select);

			return row;
		}

		#endregion

		#region Grid + tiles

		private List<TileInfo> BuildTileInfos()
		{
			var list = new List<TileInfo>();
			if (uiWidgetsAsset?.widgets == null) return list;

			foreach (var w in uiWidgetsAsset.widgets)
			{
				string cat = GetCategoryDisplayName(w.category);
				if (w.widgetPrefab != null && !string.IsNullOrEmpty(w.widgetName))
					list.Add(new TileInfo { name = w.widgetName, category = cat, prefab = w.widgetPrefab, noCanvas = w.noCanvasRequired });

				if (w.widgetVariations != null)
				{
					foreach (var v in w.widgetVariations)
					{
						if (v.widgetPrefab == null) continue;
						list.Add(new TileInfo { name = v.widgetName, category = cat, prefab = v.widgetPrefab, noCanvas = v.noCanvasRequired });
					}
				}
			}

			// Attach-component tiles appear after the widget categories.
			foreach (var c in ComponentTiles)
				list.Add(new TileInfo { name = c.name, category = c.category, component = c.type, noCanvas = false });

			var curatedGuids = GetCuratedPrefabGuids();
			foreach (var entry in ProjectUIPrefabScanCache.GetEntries())
			{
				if (entry.prefab == null || string.IsNullOrEmpty(entry.Name))
					continue;
				if (curatedGuids.Contains(entry.guid))
					continue;
				list.Add(new TileInfo
				{
					name = entry.Name,
					category = ProjectUIPrefabsCategory,
					prefab = entry.prefab,
					noCanvas = entry.noCanvasRequired
				});
			}

			return list;
		}

		private HashSet<string> GetCuratedPrefabGuids()
		{
			var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (uiWidgetsAsset?.widgets == null)
				return guids;

			void AddPrefab(GameObject prefab)
			{
				if (prefab == null) return;
				var path = AssetDatabase.GetAssetPath(prefab);
				if (string.IsNullOrEmpty(path)) return;
				var guid = AssetDatabase.AssetPathToGUID(path);
				if (!string.IsNullOrEmpty(guid))
					guids.Add(guid);
			}

			foreach (var w in uiWidgetsAsset.widgets)
			{
				AddPrefab(w.widgetPrefab);
				if (w.widgetVariations == null) continue;
				foreach (var v in w.widgetVariations)
					AddPrefab(v.widgetPrefab);
			}

			return guids;
		}

		private void RebuildGrid()
		{
			if (_gridContainer == null) return;
			_gridContainer.Clear();

			bool searching = !string.IsNullOrEmpty(searchQuery);

			var order = new List<string>();
			var byCat = new Dictionary<string, List<TileInfo>>();
			foreach (var t in BuildTileInfos())
			{
				if (!byCat.TryGetValue(t.category, out var l))
				{
					l = new List<TileInfo>();
					byCat[t.category] = l;
					order.Add(t.category);
				}
				l.Add(t);
			}

			foreach (var cat in order)
			{
				var visible = byCat[cat].Where(t => NameMatches(t.name)).ToList();
				if (visible.Count == 0) continue;

				var foldout = new Foldout { text = $"{cat}  ({visible.Count})" };
				// Set value before wiring the save callback so search-expansion doesn't clobber prefs.
				foldout.value = searching || GetFoldout(cat);
				if (!searching)
					foldout.RegisterValueChangedCallback(e => SetFoldout(cat, e.newValue));

				var wrap = new VisualElement();
				wrap.style.flexDirection = FlexDirection.Row;
				wrap.style.flexWrap = Wrap.Wrap;
				foreach (var t in visible)
					wrap.Add(BuildTile(t));

				foldout.Add(wrap);
				_gridContainer.Add(foldout);
			}
		}

		private VisualElement BuildTile(TileInfo info)
		{
			bool isComponent = info.IsComponent;

			var tile = new VisualElement
			{
				name = isComponent ? "component-tile" : "widget-tile",
				tooltip = isComponent
					? info.name + "\nAdd component to selection (or a new Canvas child)"
					: info.name + "\nClick = child · Drag = scene · Right-click = more"
			};
			tile.style.width = TileWidth;
			tile.style.height = TileHeight;
			tile.style.marginRight = 4;
			tile.style.marginBottom = 4;
			tile.style.paddingTop = 4;
			tile.style.paddingBottom = 4;
			tile.style.alignItems = Align.Center;
			tile.style.justifyContent = Justify.Center;
			SetBorderRadius(tile, 4);
			// Component tiles get a warm tint so "attach" reads differently from "spawn".
			var idle = isComponent ? new Color(1f, 0.78f, 0.35f, 0.06f) : new Color(1f, 1f, 1f, 0.04f);
			var hover = isComponent ? new Color(1f, 0.78f, 0.35f, 0.16f) : new Color(1f, 1f, 1f, 0.10f);
			tile.style.backgroundColor = idle;
			tile.RegisterCallback<MouseEnterEvent>(_ =>
			{
				tile.style.backgroundColor = hover;
				UIWidgetsHierarchyHighlight.Set(ResolveHighlightTarget(info.noCanvas));
			});
			tile.RegisterCallback<MouseLeaveEvent>(_ =>
			{
				tile.style.backgroundColor = idle;
				UIWidgetsHierarchyHighlight.Clear();
			});

			var img = new UIEImage { scaleMode = ScaleMode.ScaleToFit };
			img.style.width = TileIconSize;
			img.style.height = TileIconSize;
			AssignIcon(img, info);
			tile.Add(img);

			var label = new Label(info.name);
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.fontSize = 10;
			label.style.whiteSpace = WhiteSpace.Normal;
			label.style.overflow = Overflow.Hidden;
			label.style.width = Length.Percent(100);
			label.style.marginTop = 2;
			tile.Add(label);

			if (isComponent)
			{
				// Corner badge marks the tile as "attach component", not "spawn prefab".
				var badge = new Label("⊕") { tooltip = "Adds a component to the selection" };
				badge.style.position = Position.Absolute;
				badge.style.top = 1;
				badge.style.right = 4;
				badge.style.fontSize = 12;
				badge.style.color = new Color(1f, 0.78f, 0.35f, 1f);
				tile.Add(badge);
			}

			if (isComponent)
				WireComponentInteractions(tile, info);
			else
				WireSpawnInteractions(tile, info);
			return tile;
		}

		// One coherent icon set: each widget resolves to a Unity component's own icon (via
		// ObjectContent), so the palette reads uniformly without fragile string icon-names that
		// spam "Unable to load the icon" for anything Unity doesn't recognise.
		private static readonly Dictionary<string, System.Type> WidgetIconTypes = new Dictionary<string, System.Type>
		{
			{ "Panel", typeof(UnityEngine.UI.Image) },
			{ "Canvas", typeof(Canvas) },
			{ "ButtonX", typeof(UnityEngine.UI.Button) },
			{ "ButtonTMP", typeof(UnityEngine.UI.Button) },
			{ "DropDown", typeof(UnityEngine.UI.Dropdown) },
			{ "Image", typeof(UnityEngine.UI.Image) },
			{ "RawImage", typeof(UnityEngine.UI.RawImage) },
			{ "InputField", typeof(UnityEngine.UI.InputField) },
			{ "Slider", typeof(UnityEngine.UI.Slider) },
			{ "BoxSlider", typeof(UnityEngine.UI.Slider) },
			{ "MinMaxSlider", typeof(UnityEngine.UI.Slider) },
			{ "RadialSlider", typeof(UnityEngine.UI.Slider) },
			{ "RangeSlider", typeof(UnityEngine.UI.Slider) },
			{ "Stepper", typeof(UnityEngine.UI.Slider) },
			{ "Text", typeof(UnityEngine.UI.Text) },
			{ "Toggle", typeof(UnityEngine.UI.Toggle) },
			{ "Toggle Image", typeof(UnityEngine.UI.Toggle) },
			{ "Tooltip", typeof(UnityEngine.UI.Text) },
			{ "TooltipTrigger", typeof(UnityEngine.UI.Text) },
			{ "Setup Default State", typeof(GameObject) },
			{ "EventSystem", typeof(UnityEngine.EventSystems.EventSystem) },
			{ "ScollableList", typeof(UnityEngine.UI.ScrollRect) },
			{ "ScrollList_Horizontal", typeof(UnityEngine.UI.ScrollRect) },
			{ "ScrollList_Vertical", typeof(UnityEngine.UI.ScrollRect) },
			{ "Tabs", typeof(UnityEngine.UI.Toggle) },
			{ "Cards", typeof(UnityEngine.UI.Image) },
			{ "CardExpanding", typeof(UnityEngine.UI.Image) },
			{ "CardPopup", typeof(UnityEngine.UI.Image) },
			{ "CardStack", typeof(UnityEngine.UI.Image) },
			{ "Dialog Screen", typeof(Canvas) },
			{ "Fader Sceen", typeof(Canvas) },
			{ "Input Dialog Screen", typeof(Canvas) },
			{ "Loading Screen", typeof(Canvas) },
			{ "Wait Screen", typeof(Canvas) },
			{ "Line Message Screen", typeof(Canvas) },
			{ "Toast Message Canvas", typeof(Canvas) },
		};

		private void AssignIcon(UIEImage img, TileInfo info)
		{
			img.image = GetPaletteIcon(info);
		}

		private static Texture GetPaletteIcon(TileInfo info)
		{
			// Component tiles: the component type's own icon (custom scripts get the C# script icon).
			if (info.IsComponent)
				return TypeIcon(info.component);

			if (!string.IsNullOrEmpty(info.name) && WidgetIconTypes.TryGetValue(info.name, out var type))
			{
				var tex = TypeIcon(type);
				if (tex != null) return tex;
			}

			// Unmapped widget: the prefab's own mini thumbnail, else the generic GameObject icon.
			Texture mini = info.prefab != null ? AssetPreview.GetMiniThumbnail(info.prefab) : null;
			return mini != null ? mini : TypeIcon(typeof(GameObject));
		}

		private static Texture TypeIcon(System.Type type)
		{
			if (type == null) return null;
			var content = EditorGUIUtility.ObjectContent(null, type);
			return content != null ? content.image : null;
		}

		private void WireComponentInteractions(VisualElement tile, TileInfo info)
		{
			// Attach-only: click adds the component; right-click offers the same. No drag/scene drop.
			tile.RegisterCallback<PointerUpEvent>(e =>
			{
				if (e.button != 0) return;
				UIWidgetsHierarchyHighlight.Clear();
				AttachOrAddToCanvas(info.component, info.name);
			});

			tile.AddManipulator(new ContextualMenuManipulator(evt =>
				evt.menu.AppendAction("Add to Selection", _ =>
				{
					UIWidgetsHierarchyHighlight.Clear();
					AttachOrAddToCanvas(info.component, info.name);
				})));
		}

		private void WireSpawnInteractions(VisualElement tile, TileInfo info)
		{
			Vector2 down = Vector2.zero;
			bool armed = false;

			tile.RegisterCallback<PointerDownEvent>(e =>
			{
				if (e.button != 0) return;
				down = (Vector2)e.position;
				armed = true;
				tile.CapturePointer(e.pointerId);
			});

			tile.RegisterCallback<PointerMoveEvent>(e =>
			{
				if (!armed) return;
				if (((Vector2)e.position - down).magnitude < 6f) return;
				armed = false;
				if (tile.HasPointerCapture(e.pointerId))
					tile.ReleasePointer(e.pointerId);
				StartWidgetDrag(info);
			});

			tile.RegisterCallback<PointerUpEvent>(e =>
			{
				bool wasArmed = armed;
				armed = false;
				if (tile.HasPointerCapture(e.pointerId))
					tile.ReleasePointer(e.pointerId);
				if (wasArmed && e.button == 0)
				{
					UIWidgetsHierarchyHighlight.Clear();
					CreateAsChild(info.prefab, info.noCanvas);
					PushRecent(info.name);
				}
			});

			tile.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction("Add as Child", _ => { UIWidgetsHierarchyHighlight.Clear(); CreateAsChild(info.prefab, info.noCanvas); PushRecent(info.name); });
				evt.menu.AppendAction("Add as Sibling", _ => { UIWidgetsHierarchyHighlight.Clear(); CreateAsSibling(info.prefab, info.noCanvas); PushRecent(info.name); });
				evt.menu.AppendAction("Add as Parent", _ => { UIWidgetsHierarchyHighlight.Clear(); CreateAsParent(info.prefab, info.noCanvas); PushRecent(info.name); });
			}));
		}

		/// <summary>The GameObject a left-click (CreateAsChild) would parent the new widget under,
		/// or null when it would create a fresh Canvas (nothing to highlight yet).</summary>
		private GameObject ResolveHighlightTarget(bool noCanvas)
		{
			var sel = Selection.activeGameObject;
			if (sel != null) return sel;
			if (noCanvas) return null;
			if (preferExistingCanvas)
			{
				var canvas = FindFirstObjectByType<Canvas>();
				return canvas != null ? canvas.gameObject : null;
			}
			return null;
		}

		private void StartWidgetDrag(TileInfo info)
		{
			if (info.prefab == null) return;
			UIWidgetsHierarchyHighlight.Clear();
			DragAndDrop.PrepareStartDrag();
			DragAndDrop.objectReferences = new UnityEngine.Object[] { info.prefab };
			DragAndDrop.SetGenericData(DragGenericDataKey, true);
			DragAndDrop.SetGenericData(DragNoCanvasKey, info.noCanvas);
			DragAndDrop.StartDrag("UI Widget: " + info.name);
		}

		private static void SetBorderRadius(VisualElement e, float r)
		{
			e.style.borderTopLeftRadius = r;
			e.style.borderTopRightRadius = r;
			e.style.borderBottomLeftRadius = r;
			e.style.borderBottomRightRadius = r;
		}

		#endregion

		#region Recents

		private VisualElement BuildRecentsSection()
		{
			var section = new VisualElement();
			var header = new Label("Recent");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.fontSize = 11;
			header.style.marginTop = 2;
			section.Add(header);

			_recentsStrip = new VisualElement();
			_recentsStrip.style.flexDirection = FlexDirection.Row;
			_recentsStrip.style.flexWrap = Wrap.Wrap;
			section.Add(_recentsStrip);
			return section;
		}

		private List<string> GetRecents()
		{
			var raw = EditorPrefs.GetString(PrefKey_Recents, string.Empty);
			return string.IsNullOrEmpty(raw)
				? new List<string>()
				: raw.Split(RecentsDelim).Where(x => !string.IsNullOrEmpty(x)).ToList();
		}

		private void PushRecent(string name)
		{
			if (string.IsNullOrEmpty(name)) return;
			var list = GetRecents();
			list.RemoveAll(x => x == name);
			list.Insert(0, name);
			while (list.Count > RecentsMax)
				list.RemoveAt(list.Count - 1);
			EditorPrefs.SetString(PrefKey_Recents, string.Join(RecentsDelim.ToString(), list));
			RefreshRecents();
		}

		private void RefreshRecents()
		{
			if (_recentsStrip == null || _recentsSection == null) return;
			_recentsStrip.Clear();

			var byName = new Dictionary<string, TileInfo>();
			foreach (var t in BuildTileInfos())
				byName[t.name] = t;

			int shown = 0;
			foreach (var n in GetRecents())
			{
				if (byName.TryGetValue(n, out var info))
				{
					_recentsStrip.Add(BuildTile(info));
					shown++;
				}
			}
			_recentsSection.style.display = shown > 0 ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private static bool GetFoldout(string cat) => EditorPrefs.GetBool(PrefKey_FoldoutPrefix + cat, true);
		private static void SetFoldout(string cat, bool value) => EditorPrefs.SetBool(PrefKey_FoldoutPrefix + cat, value);

		#endregion
	}
}
