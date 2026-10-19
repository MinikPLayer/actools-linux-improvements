using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Primitives;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Closes other popups from the same group when a popup opens (closing on outside clicks is handled by Avalonia’s light dismiss).
    /// </summary>
    public class PopupHelper {
        public static void Initialize() { }

        public static bool GetRegister(DependencyObject obj) {
            return obj.GetValue(RegisterProperty) as bool? == true;
        }

        public static void SetRegister(DependencyObject obj, bool value) {
            obj.SetValue(RegisterProperty, value);
        }

        public static readonly AttachedProperty<bool> RegisterProperty = DP.RegisterAttached<bool>("Register",
                typeof(PopupHelper), new UIPropertyMetadata(OnRegisterChanged));

        private static void OnRegisterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is Popup popup) || !(e.NewValue is bool)) return;
            var newValue = (bool)e.NewValue;

            popup.Opened -= PopupOpened;
            popup.Closed -= PopupClosed;

            if (newValue) {
                popup.Opened += PopupOpened;
                popup.Closed += PopupClosed;
            }
        }

        public static int GetGroup(DependencyObject obj) {
            return obj.GetValue(GroupProperty) as int? ?? 0;
        }

        public static void SetGroup(DependencyObject obj, int value) {
            obj.SetValue(GroupProperty, value);
        }

        public static readonly AttachedProperty<int> GroupProperty = DP.RegisterAttached<int>("Group",
                typeof(PopupHelper), new UIPropertyMetadata(0));

        private static readonly List<WeakReference<Popup>> Opened = new List<WeakReference<Popup>>();

        private static IEnumerable<Popup> GetOpenPopups() {
            Opened.RemoveAll(x => !x.TryGetTarget(out var p) || !p.IsOpen);
            return Opened.Select(x => x.TryGetTarget(out var p) ? p : null).Where(x => x != null).ToList();
        }

        private static void PopupOpened(object sender, EventArgs eventArgs) {
            if (!(sender is Popup popup)) return;

            var group = GetGroup(popup);
            foreach (var openPopup in GetOpenPopups().Where(x => !ReferenceEquals(x, popup) && (group == 0 || (group & GetGroup(x)) != 0))) {
                openPopup.IsOpen = false;
            }

            Opened.Add(new WeakReference<Popup>(popup));
        }

        private static void PopupClosed(object sender, EventArgs e) {
            if (!(sender is Popup popup)) return;
            Opened.RemoveAll(x => !x.TryGetTarget(out var p) || ReferenceEquals(p, popup));
        }
    }
}
