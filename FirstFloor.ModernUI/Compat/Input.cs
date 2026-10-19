using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace System.Windows.Input {
    public enum MouseButtonState {
        Released,
        Pressed
    }

    /// <summary>
    /// WPF’s Keyboard and Mouse static classes: tracks the state through class handlers.
    /// </summary>
    public static class Keyboard {
        private static bool _hooked;

        public static KeyModifiers Modifiers { get; internal set; }

        public static IInputElement FocusedElement => (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.FocusManager?.GetFocusedElement();

        public static void Hook() {
            if (_hooked) return;
            _hooked = true;

            InputElement.KeyDownEvent.AddClassHandler<TopLevel>((s, e) => Modifiers = ((KeyEventArgs)e).KeyModifiers, RoutingStrategies.Tunnel, true);
            InputElement.KeyUpEvent.AddClassHandler<TopLevel>((s, e) => Modifiers = ((KeyEventArgs)e).KeyModifiers, RoutingStrategies.Tunnel, true);
            InputElement.PointerPressedEvent.AddClassHandler<TopLevel>((s, e) => OnPointer(e), RoutingStrategies.Tunnel, true);
            InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>((s, e) => OnPointer(e), RoutingStrategies.Tunnel, true);
            InputElement.PointerMovedEvent.AddClassHandler<TopLevel>((s, e) => OnPointer(e), RoutingStrategies.Tunnel, true);
        }

        private static void OnPointer(RoutedEventArgs routed) {
            if (routed is PointerEventArgs e) {
                Modifiers = e.KeyModifiers;
                Mouse.Update(e);
            }
        }

        public static IInputElement Focus(IInputElement element) {
            (element as InputElement)?.Focus();
            return element;
        }

        public static void ClearFocus() {
            (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow?.FocusManager?.ClearFocus();
        }

        public static bool IsKeyDown(Key key) {
            return false;
        }
    }

    public static class Mouse {
        public static MouseButtonState LeftButton { get; private set; }
        public static MouseButtonState RightButton { get; private set; }
        public static MouseButtonState MiddleButton { get; private set; }

        public static Cursor OverrideCursor { get; set; }

        internal static IInputElement LastOver { get; set; }

        internal static void Update(PointerEventArgs e) {
            Keyboard.Hook();
            LastOver = e.Source as IInputElement ?? LastOver;
            var p = e.GetCurrentPoint(null).Properties;
            LeftButton = p.IsLeftButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
            RightButton = p.IsRightButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
            MiddleButton = p.IsMiddleButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
        }
    }

    /// <summary>
    /// WPF-like members for Avalonia’s pointer events.
    /// </summary>
    public static class PointerEventArgsExtension {
        extension(PointerEventArgs e) {
            public MouseButton ChangedButton {
                get {
                    var kind = e.GetCurrentPoint(null).Properties.PointerUpdateKind;
                    switch (kind) {
                        case PointerUpdateKind.RightButtonPressed:
                        case PointerUpdateKind.RightButtonReleased:
                            return MouseButton.Right;
                        case PointerUpdateKind.MiddleButtonPressed:
                        case PointerUpdateKind.MiddleButtonReleased:
                            return MouseButton.Middle;
                        case PointerUpdateKind.XButton1Pressed:
                        case PointerUpdateKind.XButton1Released:
                            return MouseButton.XButton1;
                        case PointerUpdateKind.XButton2Pressed:
                        case PointerUpdateKind.XButton2Released:
                            return MouseButton.XButton2;
                        default:
                            return MouseButton.Left;
                    }
                }
            }

            public int ClickCount => e is PointerPressedEventArgs p ? p.ClickCount : 1;

            public MouseButtonState LeftButton => e.GetCurrentPoint(null).Properties.IsLeftButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
            public MouseButtonState RightButton => e.GetCurrentPoint(null).Properties.IsRightButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
            public MouseButtonState MiddleButton => e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
        }

        extension(PointerWheelEventArgs e) {
            public int Delta => (int)(e.Delta.Y * 120d);
        }

        extension(KeyEventArgs e) {
            public Key SystemKey => e.Key;
        }
    }

    /// <summary>
    /// A key binding, compatible with WPF’s InputBinding (but keyboard only).
    /// </summary>
    public class InputBinding : KeyBinding {
        public InputBinding() { }

        public InputBinding(ICommand command, KeyGesture gesture) {
            Command = command;
            Gesture = gesture;
        }
    }

    public static class InputBindingsExtension {
        extension(Avalonia.Controls.Control c) {
            public List<KeyBinding> InputBindings => c.KeyBindings;
        }
    }
}

namespace System.Windows.Input {
    public static class PointerButtonExtension {
        /// <summary>
        /// Checks which button caused the pointer event.
        /// </summary>
        public static bool IsButton(this PointerEventArgs e, MouseButton button) {
            return e.ChangedButton == button;
        }
    }
}
