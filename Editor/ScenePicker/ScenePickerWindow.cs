using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIWidgets.Editor
{
	/// <summary>
	/// UIToolkit dropdown listing the objects under the cursor, front to back.
	/// Supports name search, "t:ComponentType" queries and an All/UI/2D/3D filter strip;
	/// footer runs the anchor fitter.
	/// </summary>
	public class ScenePickerWindow : EditorWindow
	{
		const float RowHeight = 20f;
		const float Width = 280f;
		const float MaxHeight = 420f;

		// Remembered for the editor session; seeded from the project-wide default.
		static UIWidgetsSettings.PickerFilter? sessionFilter;

		List<GameObject> allItems = new List<GameObject>();
		readonly List<GameObject> filtered = new List<GameObject>();
		readonly List<ToolbarToggle> filterToggles = new List<ToolbarToggle>();
		ListView listView;
		string currentQuery = string.Empty;

		static UIWidgetsSettings.PickerFilter CurrentFilter
		{
			get => sessionFilter ?? UIWidgetsSettings.instance.DefaultFilter;
			set => sessionFilter = value;
		}

		public static void Open(Vector2 screenPoint, IReadOnlyList<GameObject> items)
		{
			var window = CreateInstance<ScenePickerWindow>();
			window.allItems = new List<GameObject>(items);

			var height = Mathf.Min(MaxHeight, (items.Count * RowHeight) + 82f);
			window.ShowAsDropDown(new Rect(screenPoint, Vector2.zero), new Vector2(Width, height));
		}

		void CreateGUI()
		{
			var root = rootVisualElement;

			var search = new ToolbarSearchField();
			search.style.width = Length.Percent(100);
			search.style.marginTop = 2;
			search.style.marginBottom = 2;
			search.RegisterValueChangedCallback(evt =>
			{
				currentQuery = evt.newValue;
				Refresh();
			});
			root.Add(search);

			root.Add(BuildFilterStrip());

			ApplyFilterToList();

			listView = new ListView(filtered, RowHeight, MakeRow, BindRow)
			{
				selectionType = SelectionType.Single,
				style = { flexGrow = 1 },
			};
			listView.selectionChanged += OnRowChosen;
			root.Add(listView);

			var footer = new Button(FitAnchorsOnSelection) { text = "Fit Anchors (Alt+O)" };
			footer.style.marginBottom = 2;
			root.Add(footer);

			root.RegisterCallback<KeyDownEvent>(evt =>
			{
				if (evt.keyCode == KeyCode.Escape)
				{
					Close();
				}
			});

			search.schedule.Execute(() => search.Focus());
		}

		VisualElement MakeRow()
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					paddingLeft = 4,
				},
			};
			row.Add(new Image { name = "icon", style = { width = 16, height = 16, marginRight = 4 } });
			row.Add(new Label { name = "label" });

			row.RegisterCallback<PointerEnterEvent>(_ =>
			{
				if (row.userData is GameObject go)
				{
					ScenePickerTrigger.Hovered = go;
					if (UIWidgetsSettings.instance.HoverPingsHierarchy)
					{
						EditorGUIUtility.PingObject(go);
					}
				}
			});
			row.RegisterCallback<PointerLeaveEvent>(_ => ScenePickerTrigger.Hovered = null);

			return row;
		}

		void BindRow(VisualElement row, int index)
		{
			var go = filtered[index];
			row.userData = go;
			row.Q<Image>("icon").image = EditorGUIUtility.ObjectContent(go, typeof(GameObject)).image;

			var label = row.Q<Label>("label");
			label.text = go.name;
			label.tooltip = ComponentSummary(go);
		}

		static string ComponentSummary(GameObject go)
		{
			var components = go.GetComponents<Component>();
			var names = new List<string>(components.Length);
			foreach (var component in components)
			{
				if (component != null)
				{
					names.Add(component.GetType().Name);
				}
			}

			return string.Join(", ", names);
		}

		void OnRowChosen(IEnumerable<object> selection)
		{
			foreach (var item in selection)
			{
				if (item is GameObject go)
				{
					Selection.activeGameObject = go;
					EditorGUIUtility.PingObject(go);
					ScenePickerTrigger.Hovered = null;
					Close();
					return;
				}
			}
		}

		VisualElement BuildFilterStrip()
		{
			var strip = new Toolbar();
			strip.style.flexShrink = 0;
			filterToggles.Clear();

			AddFilterToggle(strip, "All", UIWidgetsSettings.PickerFilter.All);
			AddFilterToggle(strip, "UI", UIWidgetsSettings.PickerFilter.UI);
			AddFilterToggle(strip, "2D", UIWidgetsSettings.PickerFilter.TwoD);
			AddFilterToggle(strip, "3D", UIWidgetsSettings.PickerFilter.ThreeD);

			return strip;
		}

		void AddFilterToggle(Toolbar strip, string label, UIWidgetsSettings.PickerFilter filter)
		{
			var toggle = new ToolbarToggle { text = label, value = CurrentFilter == filter };
			toggle.style.flexGrow = 1;
			toggle.RegisterValueChangedCallback(evt =>
			{
				if (!evt.newValue)
				{
					// Acts like a radio group: re-clicking the active tab keeps it on.
					toggle.SetValueWithoutNotify(true);
					return;
				}

				CurrentFilter = filter;
				foreach (var other in filterToggles)
				{
					if (other != toggle)
					{
						other.SetValueWithoutNotify(false);
					}
				}

				Refresh();
			});

			filterToggles.Add(toggle);
			strip.Add(toggle);
		}

		void Refresh()
		{
			ApplyFilterToList();
			listView.RefreshItems();
		}

		void ApplyFilterToList()
		{
			filtered.Clear();

			foreach (var go in allItems)
			{
				if (MatchesCategory(go, CurrentFilter) && MatchesQuery(go, currentQuery))
				{
					filtered.Add(go);
				}
			}
		}

		static bool MatchesQuery(GameObject go, string query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return true;
			}

			if (query.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
			{
				return HasComponentNamed(go, query.Substring(2).Trim());
			}

			return go.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		static bool MatchesCategory(GameObject go, UIWidgetsSettings.PickerFilter filter)
		{
			return filter switch
			{
				UIWidgetsSettings.PickerFilter.UI => IsUI(go),
				UIWidgetsSettings.PickerFilter.TwoD => !IsUI(go) && Is2D(go),
				UIWidgetsSettings.PickerFilter.ThreeD => !IsUI(go) && !Is2D(go),
				_ => true,
			};
		}

		static bool IsUI(GameObject go) => go.transform is RectTransform;

		static bool Is2D(GameObject go)
		{
			return go.GetComponent<SpriteRenderer>() != null
				|| go.GetComponent<SpriteMask>() != null
				|| go.GetComponent<Collider2D>() != null
				|| go.GetComponent<Rigidbody2D>() != null
				|| go.GetComponent<UnityEngine.Rendering.SortingGroup>() != null;
		}

		static bool HasComponentNamed(GameObject go, string typeName)
		{
			if (typeName.Length == 0)
			{
				return true;
			}

			foreach (var component in go.GetComponents<Component>())
			{
				if (component != null && component.GetType().Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}

		void FitAnchorsOnSelection()
		{
			AnchorTools.FitAnchorsToCorners();
			Close();
		}

		void OnDisable()
		{
			ScenePickerTrigger.Hovered = null;
		}
	}
}
