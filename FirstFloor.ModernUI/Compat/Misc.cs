using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
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

        extension(Control c) {
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
