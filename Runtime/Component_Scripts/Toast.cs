using System;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
	public enum ToastColor
	{
		Black,
		Red,
		Purple,
		Magenta,
		Blue,
		Green,
		Yellow,
		Orange
	}

	public enum ToastPosition
	{
		TopLeft,
		TopCenter,
		TopRight,
		MiddleLeft,
		MiddleCenter,
		MiddleRight,
		BottomLeft,
		BottomCenter,
		BottomRight
	}

	/// <summary>Maps common feedback severity to <see cref="ToastColor"/>.</summary>
	public enum ToastSeverity
	{
		Info,
		Success,
		Warning,
		Error
	}

	public static class Toast
	{
		/// <summary>
		/// Resolves the scene <see cref="ToastUI"/> singleton (place
		/// <c>GameObject → UI (Canvas) → Singletons → Toast Message Canvas</c>).
		/// Missing instance already logs via <see cref="PersistentSingletonBehaviour{T}"/>.
		/// </summary>
		private static ToastUI Resolve()
		{
			return ToastUI.Instance;
		}

		public static ToastBuilder Create(string text)
		{
			return new ToastBuilder(text);
		}

		public class ToastBuilder
		{
			private readonly string _text;
			private float _duration = 2f;
			private ToastColor _color = ToastColor.Black;
			private Color? _customColor;
			private ToastPosition _position = ToastPosition.BottomCenter;
			private bool _clickToDismiss;
			private Action _onDismiss;
			private Sprite _icon;
			private bool _replace;

			internal ToastBuilder(string text)
			{
				_text = text;
			}

			public ToastBuilder WithDuration(float duration)
			{
				_duration = duration;
				return this;
			}

			public ToastBuilder WithColor(ToastColor color)
			{
				_color = color;
				_customColor = null;
				return this;
			}

			public ToastBuilder WithColor(Color color)
			{
				_customColor = color;
				return this;
			}

			public ToastBuilder WithSeverity(ToastSeverity severity)
			{
				_customColor = null;
				_color = SeverityToColor(severity);
				return this;
			}

			public ToastBuilder AtPosition(ToastPosition position)
			{
				_position = position;
				return this;
			}

			public ToastBuilder ClickToDismiss(bool enable)
			{
				_clickToDismiss = enable;
				return this;
			}

			public ToastBuilder OnDismiss(Action callback)
			{
				_onDismiss = callback;
				return this;
			}

			public ToastBuilder WithIcon(Sprite icon)
			{
				_icon = icon;
				return this;
			}

			/// <summary>Clear the queue and replace any showing toast with this one.</summary>
			public ToastBuilder Replace()
			{
				_replace = true;
				return this;
			}

			public void Show()
			{
				var ui = Resolve();
				if (ui == null)
				{
					return;
				}

				Color color = _customColor.HasValue ? _customColor.Value : ui.ResolveColor(_color);
				ui.Enqueue(ToastRequest.Create(
					_text,
					_duration,
					color,
					_position,
					_clickToDismiss,
					_onDismiss,
					_icon,
					_replace));
			}

			private static ToastColor SeverityToColor(ToastSeverity severity)
			{
				switch (severity)
				{
					case ToastSeverity.Info: return ToastColor.Blue;
					case ToastSeverity.Success: return ToastColor.Green;
					case ToastSeverity.Warning: return ToastColor.Orange;
					case ToastSeverity.Error: return ToastColor.Red;
					default:
						throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
				}
			}
		}

		public static void Show(string text)
		{
			Create(text).Show();
		}

		public static void Show(string text, float duration)
		{
			Create(text).WithDuration(duration).Show();
		}

		public static void Show(string text, float duration, ToastPosition position)
		{
			Create(text).WithDuration(duration).AtPosition(position).Show();
		}

		public static void Show(string text, ToastColor color)
		{
			Create(text).WithColor(color).Show();
		}

		public static void Show(string text, ToastColor color, ToastPosition position)
		{
			Create(text).WithColor(color).AtPosition(position).Show();
		}

		public static void Show(string text, Color color)
		{
			Create(text).WithColor(color).Show();
		}

		public static void Show(string text, Color color, ToastPosition position)
		{
			Create(text).WithColor(color).AtPosition(position).Show();
		}

		public static void Show(string text, float duration, ToastColor color)
		{
			Create(text).WithDuration(duration).WithColor(color).Show();
		}

		public static void Show(string text, float duration, ToastColor color, ToastPosition position)
		{
			Create(text).WithDuration(duration).WithColor(color).AtPosition(position).Show();
		}

		public static void Show(string text, float duration, Color color)
		{
			Create(text).WithDuration(duration).WithColor(color).Show();
		}

		public static void Show(string text, float duration, Color color, ToastPosition position)
		{
			Create(text).WithDuration(duration).WithColor(color).AtPosition(position).Show();
		}

		public static void Dismiss()
		{
			if (ToastUI.HasInstance)
			{
				ToastUI.Instance.Dismiss();
			}
		}

		public static void DismissAll()
		{
			if (ToastUI.HasInstance)
			{
				ToastUI.Instance.DismissAll();
			}
		}
	}
}
