using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AetherNexus.UIWidgets.Editor
{
	/// <summary>
	/// Builds stock Unity UI controls for the palette Default UI section (no curated prefabs).
	/// Mix: TMP for Text / Input Field / Dropdown; DefaultControls for the rest.
	/// </summary>
	internal static class DefaultUiFactory
	{
		private const string kUISpritePath = "UI/Skin/UISprite.psd";
		private const string kBackgroundPath = "UI/Skin/Background.psd";
		private const string kInputFieldPath = "UI/Skin/InputFieldBackground.psd";
		private const string kKnobPath = "UI/Skin/Knob.psd";
		private const string kCheckmarkPath = "UI/Skin/Checkmark.psd";
		private const string kDropdownPath = "UI/Skin/DropdownArrow.psd";
		private const string kMaskPath = "UI/Skin/UIMask.psd";

		private static DefaultControls.Resources s_Resources;
		private static bool s_ResourcesReady;

		internal static DefaultControls.Resources GetStandardResources()
		{
			if (s_ResourcesReady)
				return s_Resources;

			s_Resources = new DefaultControls.Resources
			{
				standard = AssetDatabase.GetBuiltinExtraResource<Sprite>(kUISpritePath),
				background = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundPath),
				inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>(kInputFieldPath),
				knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath),
				checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>(kCheckmarkPath),
				dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>(kDropdownPath),
				mask = AssetDatabase.GetBuiltinExtraResource<Sprite>(kMaskPath)
			};
			s_ResourcesReady = true;
			return s_Resources;
		}

		internal static TMP_DefaultControls.Resources GetTmpResources()
		{
			var ui = GetStandardResources();
			return new TMP_DefaultControls.Resources
			{
				standard = ui.standard,
				background = ui.background,
				inputField = ui.inputField,
				knob = ui.knob,
				checkmark = ui.checkmark,
				dropdown = ui.dropdown,
				mask = ui.mask
			};
		}

		internal static GameObject CreateCanvas()
		{
			var canvasGO = new GameObject("Canvas");
			canvasGO.layer = LayerMask.NameToLayer("UI");
			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();
			return canvasGO;
		}

		internal static GameObject CreateEventSystem()
		{
			var es = new GameObject("EventSystem");
			es.AddComponent<EventSystem>();
			es.AddComponent<StandaloneInputModule>();
			return es;
		}

		internal static GameObject CreateImage() => DefaultControls.CreateImage(GetStandardResources());
		internal static GameObject CreateRawImage() => DefaultControls.CreateRawImage(GetStandardResources());
		internal static GameObject CreateButton() => DefaultControls.CreateButton(GetStandardResources());
		internal static GameObject CreateToggle() => DefaultControls.CreateToggle(GetStandardResources());
		internal static GameObject CreateSlider() => DefaultControls.CreateSlider(GetStandardResources());
		internal static GameObject CreateScrollbar() => DefaultControls.CreateScrollbar(GetStandardResources());
		internal static GameObject CreateScrollView() => DefaultControls.CreateScrollView(GetStandardResources());
		internal static GameObject CreateText() => TMP_DefaultControls.CreateText(GetTmpResources());
		internal static GameObject CreateInputField() => TMP_DefaultControls.CreateInputField(GetTmpResources());
		internal static GameObject CreateDropdown() => TMP_DefaultControls.CreateDropdown(GetTmpResources());

		/// <summary>Hardcoded Default UI palette entries (de-duped vs curated package widgets).</summary>
		internal static readonly (string name, bool noCanvas, Func<GameObject> factory, Type iconType)[] Tiles =
		{
			("Canvas", true, CreateCanvas, typeof(Canvas)),
			("Event System", true, CreateEventSystem, typeof(EventSystem)),
			("Image", false, CreateImage, typeof(Image)),
			("Raw Image", false, CreateRawImage, typeof(RawImage)),
			("Text - TextMeshPro", false, CreateText, typeof(TextMeshProUGUI)),
			("Button", false, CreateButton, typeof(Button)),
			("Toggle", false, CreateToggle, typeof(Toggle)),
			("Slider", false, CreateSlider, typeof(Slider)),
			("Scrollbar", false, CreateScrollbar, typeof(Scrollbar)),
			("Scroll View", false, CreateScrollView, typeof(ScrollRect)),
			("Dropdown - TextMeshPro", false, CreateDropdown, typeof(TMP_Dropdown)),
			("Input Field - TextMeshPro", false, CreateInputField, typeof(TMP_InputField)),
		};
	}
}
