using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace System.Windows {
    /// <summary>
    /// Data format names of WPF, mapped to Avalonia’s (formats of drag’n’drop and clipboard).
    /// </summary>
    public static class DataFormats {
        public const string FileDrop = "Files";
        public const string UnicodeText = "Text";
        public const string Text = "Text";
    }

    public static class ContextMenuExtension {
        /// <summary>
        /// WPF opens a context menu with IsOpen = true at the mouse position, Avalonia needs a target control.
        /// </summary>
        public static void ShowAtPointer(this ContextMenu menu) {
            var lifetime = Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var target = lifetime?.Windows.FirstOrDefault(x => x.IsActive) ?? lifetime?.MainWindow;
            if (target != null) {
                menu.Placement = PlacementMode.Pointer;
                menu.Open(target);
            }
        }
    }

    public static class ResourceProviderExtension {
        /// <summary>
        /// Keys of a merged dictionary (WPF’s ResourceDictionary is always a dictionary, Avalonia’s merged ones can be includes).
        /// </summary>
        public static IEnumerable<object> GetKeys(this Avalonia.Controls.IResourceProvider provider) {
            var dictionary = (provider is Avalonia.Markup.Xaml.Styling.ResourceInclude include ? include.Loaded : provider) as Avalonia.Controls.ResourceDictionary;
            return dictionary == null ? Enumerable.Empty<object>() : dictionary.Keys.ToList();
        }
    }

    public static class MiscExtension2 {
        extension(DragEventArgs e) {
            public DragDropEffects AllowedEffects => e.DragEffects;
        }

        extension(VectorEventArgs e) {
            public double HorizontalChange => e.Vector.X;
            public double VerticalChange => e.Vector.Y;
        }

        extension(Vector v) {
            public int Sign() => Math.Sign(v.Y);
        }

        extension(Avalonia.Controls.Control c) {
            public bool IsManipulationEnabled => false;
        }

        extension(Bitmap b) {
            public int PixelWidth => b.PixelSize.Width;
            public int PixelHeight => b.PixelSize.Height;
        }

        extension(TextBox t) {
            public void AppendText(string text) {
                t.Text += text;
                t.CaretIndex = t.Text.Length;
            }

            public bool IsReadOnlyCaretVisible {
                get => true;
                set { }
            }
        }

        extension(DataGrid g) {
            public object SelectedValue => g.SelectedItem;
        }
    }
}

namespace FirstFloor.ModernUI.Windows {
    /// <summary>
    /// Replacement of the helper which was checking Windows version (the app is also available on other platforms now).
    /// </summary>
    public static class WindowsVersionHelper {
        public static bool IsWindows10OrGreater => OperatingSystem.IsWindowsVersionAtLeast(10);
        public static bool IsWindows8Point1OrGreater => OperatingSystem.IsWindowsVersionAtLeast(6, 3);
        public static string GetVersion() => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    }
}
