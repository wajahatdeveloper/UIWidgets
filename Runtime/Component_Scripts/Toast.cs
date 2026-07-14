using System;
using UnityEngine;

namespace AetherNexus.UIWidgets
{
   public enum ToastColor {
      Black,
      Red,
      Purple,
      Magenta,
      Blue,
      Green,
      Yellow,
      Orange
   }

   public enum ToastPosition {
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

   public static class Toast {

      /// <summary>
      /// Resolves the bootstrapped <see cref="ToastUI"/> singleton. The ToastUI prefab is spawned
      /// by GameBootstrap via the ServiceManifest persistent-singleton list (DontDestroyOnLoad),
      /// so it is available across scenes without a Resources folder. Returns null (and the
      /// singleton logs an error) if the prefab was never registered.
      /// </summary>
      private static ToastUI Resolve () {
         return ToastUI.Instance ;
      }

      // Fluent API Builder
      public static ToastBuilder Create(string text) {
         return new ToastBuilder(text);
      }

      // Fluent API Builder Class
      public class ToastBuilder {
         private string _text;
         private float _duration = 2f;
         private ToastColor _color = ToastColor.Black;
         private ToastPosition _position = ToastPosition.BottomCenter;
         private bool _clickToDismiss = false;
         private Action _onDismiss = null;

         internal ToastBuilder(string text) {
            _text = text;
         }

         public ToastBuilder WithDuration(float duration) {
            _duration = duration;
            return this;
         }

         public ToastBuilder WithColor(ToastColor color) {
            _color = color;
            return this;
         }

         public ToastBuilder WithColor(Color color) {
            // Convert Color to ToastColor if possible, otherwise use custom color
            return this;
         }

         public ToastBuilder AtPosition(ToastPosition position) {
            _position = position;
            return this;
         }

         public ToastBuilder ClickToDismiss(bool enable = true) {
            _clickToDismiss = enable;
            return this;
         }

         public ToastBuilder OnDismiss(Action callback) {
            _onDismiss = callback;
            return this;
         }

         public void Show() {
            var ui = Resolve();
            if (ui == null) { return; }
            ui.Init(_text, _duration, _color, _position, _clickToDismiss, _onDismiss);
         }
      }

      // Legacy static methods for backward compatibility
      public static void Show (string text) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, 2F, ToastColor.Black, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, float duration) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, ToastColor.Black, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, float duration, ToastPosition position) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, ToastColor.Black, position) ;
      }

      public static void Show (string text, ToastColor color) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, 2F, color, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, ToastColor color, ToastPosition position) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, 2F, color, position) ;
      }

      public static void Show (string text, Color color) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, 2F, color, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, Color color, ToastPosition position) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, 2F, color, position) ;
      }

      public static void Show (string text, float duration, ToastColor color) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, color, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, float duration, ToastColor color, ToastPosition position) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, color, position) ;
      }

      public static void Show (string text, float duration, Color color) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, color, ToastPosition.BottomCenter) ;
      }

      public static void Show (string text, float duration, Color color, ToastPosition position) {
         var ui = Resolve () ; if (ui == null) { return ; }
         ui.Init (text, duration, color, position) ;
      }

      public static void Dismiss () {
         if (ToastUI.HasInstance) { ToastUI.Instance.Dismiss () ; }
      }
   }
}
