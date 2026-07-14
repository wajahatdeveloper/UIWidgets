using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AetherNexus.FoundationPlatform.DebugX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets
{
	public class AutoUIRefs : MonoBehaviour
	{
		/// <summary>Single source of truth for UI list property name, code-gen type name, and inspector section.</summary>
		public struct UIListDescriptor
		{
			public string propertyName;
			public string typeName;
			public string section;
		}

		public static readonly UIListDescriptor[] UIListDescriptors = new[]
		{
			new UIListDescriptor { propertyName = "buttons", typeName = "Button", section = "Buttons" },
			new UIListDescriptor { propertyName = "buttonXes", typeName = "ButtonX", section = "Buttons" },
			new UIListDescriptor { propertyName = "texts", typeName = "Text", section = "Texts" },
			new UIListDescriptor { propertyName = "textMeshes", typeName = "TextMeshProUGUI", section = "Texts" },
			new UIListDescriptor { propertyName = "images", typeName = "Image", section = "Other" },
			new UIListDescriptor { propertyName = "toggles", typeName = "Toggle", section = "Other" },
			new UIListDescriptor { propertyName = "inputFields", typeName = "InputField", section = "Other" },
			new UIListDescriptor { propertyName = "tmpInputFields", typeName = "TMP_InputField", section = "Other" },
			new UIListDescriptor { propertyName = "sliders", typeName = "Slider", section = "Other" },
			new UIListDescriptor { propertyName = "dropdowns", typeName = "Dropdown", section = "Other" },
			new UIListDescriptor { propertyName = "tmpDropdowns", typeName = "TMP_Dropdown", section = "Other" },
			new UIListDescriptor { propertyName = "scrollRects", typeName = "ScrollRect", section = "Other" },
			new UIListDescriptor { propertyName = "monoBehaviourScripts", typeName = "MonoBehaviour", section = "Other" },
		};

		public string scriptName;

		[Header("Scan Options")]
		[Tooltip("Include inactive GameObjects when scanning for UI components.")]
		public bool includeInactive = true;

		[Tooltip("Log discovered components during scan.")]
		public bool verboseLogging = false;

		[Tooltip("Avoid adding duplicates if the same component is encountered multiple times.")]
		public bool preventDuplicates = true;

		[Tooltip("Include non-UI MonoBehaviour scripts (excluding already-scanned UI component types).")]
		public bool includeMonoBehaviourScripts = false;

		[InspectorName("Buttons (Scene)")]
		public List<Button> buttons = new List<Button>();
		[InspectorName("ButtonX (Scene)")]
		public List<ButtonX> buttonXes = new List<ButtonX>();
		[InspectorName("Texts (Scene)")]
		public List<Text> texts = new List<Text>();
		[InspectorName("TextMeshPro UGUI (Scene)")]
		public List<TextMeshProUGUI> textMeshes = new List<TextMeshProUGUI>();
		[InspectorName("Images (Scene)")]
		public List<Image> images = new List<Image>();
		[InspectorName("Toggles (Scene)")]
		public List<Toggle> toggles = new List<Toggle>();
		[InspectorName("Input Fields (Scene)")]
		public List<InputField> inputFields = new List<InputField>();
		[InspectorName("TMP Input Fields (Scene)")]
		public List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
		[InspectorName("Sliders (Scene)")]
		public List<Slider> sliders = new List<Slider>();
		[InspectorName("Dropdowns (Scene)")]
		public List<Dropdown> dropdowns = new List<Dropdown>();
		[InspectorName("TMP Dropdowns (Scene)")]
		public List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
		[InspectorName("Scroll Rects (Scene)")]
		public List<ScrollRect> scrollRects = new List<ScrollRect>();
		[InspectorName("MonoBehaviour Scripts (Scene)")]
		public List<MonoBehaviour> monoBehaviourScripts = new List<MonoBehaviour>();
		
		private HashSet<Type> cachedExcludedScriptTypes;

		public int GetTotalCount()
		{
			int n = 0;
			foreach (var d in UIListDescriptors)
			{
				var f = GetType().GetField(d.propertyName, BindingFlags.Public | BindingFlags.Instance);
				if (f != null && f.GetValue(this) is IList list)
					n += list.Count;
			}
			return n;
		}

		public void ClearAll()
		{
			foreach (var d in UIListDescriptors)
			{
				var f = GetType().GetField(d.propertyName, BindingFlags.Public | BindingFlags.Instance);
				if (f != null && f.GetValue(this) is IList list)
					list.Clear();
			}
		}

		/// <summary>Returns the list at descriptor index for editor/code-gen. Returns null if index invalid.</summary>
		public IList GetListAt(int index)
		{
			if (index < 0 || index >= UIListDescriptors.Length) return null;
			var f = GetType().GetField(UIListDescriptors[index].propertyName, BindingFlags.Public | BindingFlags.Instance);
			return f?.GetValue(this) as IList;
		}

		private void OnValidate()
		{
			if (scriptName == String.Empty)
			{
				scriptName = gameObject.name + "UIComponents";
			}
		}

		public void FindUIElements(GameObject root)
		{
			ClearAll();
			IterateThroughChildren(root);
		}

		// Public method to start the recursive iteration
		public void IterateThroughChildren(GameObject root)
		{
			if (root != null)
			{
				IterateChildrenRecursive(root.transform);
			}
			else
			{
				DebugX.Logger(LogChannels.UI).Warning("[UI:WARN] Root GameObject is null.");
			}
		}

		// Recursive function to iterate through all children
		private void IterateChildrenRecursive(Transform parent)
		{
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				
				// Skip inactive children if includeInactive is false
				if (!includeInactive && !child.gameObject.activeInHierarchy)
				{
					continue;
				}

				if (verboseLogging)
				{
					DebugX.Logger(LogChannels.UI).Info("[UI:INFO] Child: {ChildName} (Active: {IsActive})", 
						child.name, child.gameObject.activeInHierarchy);
				}

				// Add UI components to respective lists
				AddComponentToList<Button>(child, buttons);
				AddComponentToList<ButtonX>(child, buttonXes);
				AddComponentToList<Text>(child, texts);
				AddComponentToList<TextMeshProUGUI>(child, textMeshes);
				AddComponentToList<Image>(child, images);
				AddComponentToList<Toggle>(child, toggles);
				AddComponentToList<InputField>(child, inputFields);
				AddComponentToList<TMP_InputField>(child, tmpInputFields);
				AddComponentToList<Slider>(child, sliders);
				AddComponentToList<Dropdown>(child, dropdowns);
				AddComponentToList<TMP_Dropdown>(child, tmpDropdowns);
				AddComponentToList<ScrollRect>(child, scrollRects);
				if (includeMonoBehaviourScripts)
				{
					AddMonoBehaviourScriptsToList(child, monoBehaviourScripts);
				}

				// If the child has its own children, recurse into them
				if (child.childCount > 0)
				{
					IterateChildrenRecursive(child);
				}
			}
		}

		// Helper method to find component in parent hierarchy, respecting includeInactive
		private T FindInParentHierarchy<T>(Component component) where T : Component
		{
			Transform current = component.transform.parent;
			while (current != null)
			{
				if (includeInactive || current.gameObject.activeInHierarchy)
				{
					T found = current.GetComponent<T>();
					if (found != null)
					{
						return found;
					}
				}
				current = current.parent;
			}
			return null;
		}

		// Generic method to add components to a list
		private void AddComponentToList<T>(Transform child, List<T> list) where T : Component
		{
			T component = child.GetComponent<T>();
			if (component != null)
			{
				// Don't fetch button texts
				if (component is TextMeshProUGUI || component is Text)
				{
					if (FindInParentHierarchy<Button>(component) != null ||
					    FindInParentHierarchy<ButtonX>(component) != null)
					{
						return;
					}
				}

				// Don't fetch button images
				if (component is Image)
				{
					if (FindInParentHierarchy<Button>(component) != null ||
					    FindInParentHierarchy<ButtonX>(component) != null)
					{
						return;
					}
				}

				if (preventDuplicates && list.Contains(component))
				{
					return;
				}

			list.Add(component);
			if (verboseLogging)
			{
				DebugX.Logger(LogChannels.UI).Info("[UI:INFO] Added {TypeName}: {ComponentName}", typeof(T).Name, component.name);
			}
			}
		}

		private void AddMonoBehaviourScriptsToList(Transform child, List<MonoBehaviour> list)
		{
			MonoBehaviour[] scripts = child.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour script in scripts)
			{
				if (script == null)
				{
					continue;
				}

				if (IsExcludedFromScriptScan(script.GetType()))
				{
					continue;
				}

				if (preventDuplicates && list.Contains(script))
				{
					continue;
				}

				list.Add(script);
				if (verboseLogging)
				{
					DebugX.Logger(LogChannels.UI).Info("[UI:INFO] Added {TypeName}: {ComponentName}", script.GetType().Name, script.name);
				}
			}
		}

		private bool IsExcludedFromScriptScan(Type componentType)
		{
			if (componentType == null)
			{
				return true;
			}

			if (cachedExcludedScriptTypes == null)
			{
				cachedExcludedScriptTypes = BuildExcludedScriptTypeSet();
			}

			foreach (Type excludedType in cachedExcludedScriptTypes)
			{
				if (excludedType.IsAssignableFrom(componentType))
				{
					return true;
				}
			}

			return false;
		}

		private HashSet<Type> BuildExcludedScriptTypeSet()
		{
			var excluded = new HashSet<Type>();
			foreach (var descriptor in UIListDescriptors)
			{
				if (descriptor.propertyName == "monoBehaviourScripts")
				{
					continue;
				}

				Type type = ResolveComponentType(descriptor.typeName);
				if (type != null && typeof(Component).IsAssignableFrom(type))
				{
					excluded.Add(type);
				}
			}
			return excluded;
		}

		/// <summary>Concrete type references for the known descriptor type names, to avoid an expensive multi-assembly reflection scan.</summary>
		private static readonly Dictionary<string, Type> KnownComponentTypes = new Dictionary<string, Type>
		{
			{ "Button", typeof(Button) },
			{ "ButtonX", typeof(ButtonX) },
			{ "Text", typeof(Text) },
			{ "TextMeshProUGUI", typeof(TextMeshProUGUI) },
			{ "Image", typeof(Image) },
			{ "Toggle", typeof(Toggle) },
			{ "InputField", typeof(InputField) },
			{ "TMP_InputField", typeof(TMP_InputField) },
			{ "Slider", typeof(Slider) },
			{ "Dropdown", typeof(Dropdown) },
			{ "TMP_Dropdown", typeof(TMP_Dropdown) },
			{ "ScrollRect", typeof(ScrollRect) },
			{ "MonoBehaviour", typeof(MonoBehaviour) },
		};

		private static Type ResolveComponentType(string typeName)
		{
			if (string.IsNullOrWhiteSpace(typeName))
			{
				return null;
			}

			if (KnownComponentTypes.TryGetValue(typeName, out Type known))
			{
				return known;
			}

			Type resolved = Type.GetType(typeName);
			if (resolved != null)
			{
				return resolved;
			}

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				resolved = assemblies[i].GetType(typeName);
				if (resolved != null)
				{
					return resolved;
				}

				Type[] types;
				try
				{
					types = assemblies[i].GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types;
				}

				for (int j = 0; j < types.Length; j++)
				{
					Type current = types[j];
					if (current != null && current.Name == typeName)
					{
						return current;
					}
				}
			}

			return null;
		}

		// Optional utility to scan all children under this GameObject
		[ContextMenu("Scan Children For UI Components")]
		public void ScanSelf()
		{
			FindUIElements(gameObject);
		}
	}
}