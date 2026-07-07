using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIWidgets.Editor
{
	[CreateAssetMenu(fileName = "UIWidgetsAsset", menuName = "UI Widgets/Configuration", order = 45)]
	public class UIWidgetsAssetScriptable : ScriptableObject
	{
		public string nameDelimiter = "_";
		public List<UIWidget> widgets;
	}

	[Serializable]
	public class UIWidget
	{
		public UIWidget(UIWidgetLeaf leaf)
		{
			this.widgetName = leaf.widgetName;
			this.widgetIcon = leaf.widgetIcon;
			this.widgetPrefab = leaf.widgetPrefab;
			this.noCanvasRequired = leaf.noCanvasRequired;
			this.category = leaf.category;
		}

		public string category;
		public string widgetName;
		public Texture widgetIcon;
		public GameObject widgetPrefab;
		public List<UIWidgetLeaf> widgetVariations = new List<UIWidgetLeaf>();
		public bool noCanvasRequired;
	}

	[Serializable]
	public class UIWidgetLeaf
	{
		public string category;
		public string widgetName;
		public Texture widgetIcon;
		public GameObject widgetPrefab;
		public bool noCanvasRequired;
	}
}