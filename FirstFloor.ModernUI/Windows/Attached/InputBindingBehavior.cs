using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Avalonia.VisualTree;
using FirstFloor.ModernUI.Commands;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Moves key bindings of an element to the window, so they work everywhere in it.
    /// </summary>
    public static class InputBindingBehavior {
        public static bool GetPropagateToWindow(FrameworkElement obj) {
            return obj.GetValue(PropagateToWindowProperty) as bool? == true;
        }

        public static void SetPropagateToWindow(FrameworkElement obj, bool value) {
            obj.SetValue(PropagateToWindowProperty, value);
        }

        public static readonly AttachedProperty<bool> PropagateToWindowProperty = DP.RegisterAttached<bool>("PropagateToWindow", typeof(InputBindingBehavior),
            new PropertyMetadata(false, OnPropagateToWindowChanged));

        private static void OnPropagateToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var element = (FrameworkElement)d;
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
        }

        public static void UpdateBindings(FrameworkElement obj) {
            if (obj.IsLoaded && GetPropagateToWindow(obj)) {
                RemoveBindings(obj);
                SetBindings(obj);
            }
        }

        public static Window GetWindow(DependencyObject obj) {
            return (Window)obj.GetValue(WindowProperty);
        }

        public static void SetWindow(DependencyObject obj, Window value) {
            obj.SetValue(WindowProperty, value);
        }

        public static readonly AttachedProperty<Window> WindowProperty = DP.RegisterAttached<Window>("Window",
                typeof(InputBindingBehavior), new UIPropertyMetadata(null));

        private static readonly ConditionalWeakTable<KeyBinding, FrameworkElement> Targets = new ConditionalWeakTable<KeyBinding, FrameworkElement>();
        private static readonly ConditionalWeakTable<KeyBinding, KeyBinding> Originals = new ConditionalWeakTable<KeyBinding, KeyBinding>();

        private static void SetBindings(FrameworkElement element) {
            RemoveBindings(element);

            var window = element.GetVisualRoot() as Window;
            if (window == null) return;

            SetWindow(element, window);

            for (var i = element.InputBindings.Count - 1; i >= 0; i--) {
                var binding = element.InputBindings[i];
                var wrapped = CreateBinding(window, binding);
                Targets.AddOrUpdate(wrapped, element);
                Originals.AddOrUpdate(wrapped, binding);
                window.InputBindings.Add(wrapped);
                element.InputBindings.RemoveAt(i);
            }
        }

        private static KeyBinding CreateBinding(Window window, KeyBinding baseBinding) {
            return new KeyBinding {
                Gesture = baseBinding.Gesture,
                Command = new DelegateCommand<object>(o => {
                    if (baseBinding.Gesture?.KeyModifiers.HasFlag(KeyModifiers.Alt) == true) {
                        var focused = window.FocusManager?.GetFocusedElement();
                        if (focused is TextBox) return;
                    }

                    baseBinding.Command?.Execute(baseBinding.CommandParameter);
                })
            };
        }

        private static void RemoveBindings(FrameworkElement element) {
            var window = GetWindow(element);
            if (window == null) return;

            element.ClearValue(WindowProperty);

            for (var i = window.InputBindings.Count - 1; i >= 0; i--) {
                var binding = window.InputBindings[i];
                if (Targets.TryGetValue(binding, out var target) && ReferenceEquals(target, element)) {
                    window.InputBindings.RemoveAt(i);
                    element.InputBindings.Add(Originals.TryGetValue(binding, out var original) ? original : binding);
                }
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e) {
            RemoveBindings((FrameworkElement)sender);
        }

        private static void OnLoaded(object sender, RoutedEventArgs e) {
            SetBindings((FrameworkElement)sender);
        }
    }
}
