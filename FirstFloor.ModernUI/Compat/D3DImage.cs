using System;
using Avalonia;
using Avalonia.Media;

namespace System.Windows.Interop {
    public enum D3DResourceType { IDirect3DSurface9 }

    /// <summary>
    /// Stand-in for WPF’s D3DImage. Avalonia has no Direct3D 9 interop, so the image draws nothing (see LinuxPortDocs.md);
    /// code using it (the chase camera preview) keeps compiling and just shows an empty area.
    /// </summary>
    public class D3DImage : IImage {
        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }

        public Avalonia.Size Size => new Avalonia.Size(PixelWidth, PixelHeight);

        public event EventHandler Changed { add { } remove { } }

        public void Lock() { }
        public void Unlock() { }
        public void AddDirtyRect(PixelRect rect) { }
        public void SetBackBuffer(D3DResourceType type, IntPtr backBuffer) { }

        public void Draw(DrawingContext context, Avalonia.Rect sourceRect, Avalonia.Rect destRect) { }
    }
}
