using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SharpDX.Direct3D9;
using Format = SharpDX.Direct3D9.Format;

namespace AcManager.Tools {
    public enum D3DResourceTypeEx {
        ID3D11Texture2D
    }

    /// <summary>
    /// A replacement for Interop.D3DImageEx (a mixed-mode assembly, can’t be used with .NET Core): D3DImage, but it can also show Direct3D 11
    /// textures (they need to be created with shared flag and BGRA format). Texture is shared with Direct3D 9Ex, because that’s the only thing
    /// D3DImage knows how to work with.
    /// </summary>
    public class D3DImageEx : D3DImage, IDisposable {
        private Direct3DEx _direct3D;
        private DeviceEx _device;
        private Texture _texture;
        private Surface _surface;
        private IntPtr _lastSharedHandle;

        private void EnsureDevice() {
            if (_device != null) return;

            _direct3D = new Direct3DEx();
            _device = new DeviceEx(_direct3D, 0, DeviceType.Hardware, IntPtr.Zero,
                    CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                    new PresentParameters {
                        Windowed = true,
                        SwapEffect = SwapEffect.Discard,
                        DeviceWindowHandle = GetDesktopWindow(),
                        PresentationInterval = PresentInterval.Default
                    });
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        private void ReleaseResources() {
            _surface?.Dispose();
            _surface = null;
            _texture?.Dispose();
            _texture = null;
            _lastSharedHandle = IntPtr.Zero;
        }

        public void SetBackBufferEx(D3DResourceTypeEx type, IntPtr texture) {
            if (type != D3DResourceTypeEx.ID3D11Texture2D) throw new NotSupportedException();

            Lock();
            try {
                if (texture == IntPtr.Zero) {
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                    ReleaseResources();
                    return;
                }

                IntPtr sharedHandle;
                int width, height;

                Marshal.AddRef(texture);
                using (var tex11 = new SharpDX.Direct3D11.Texture2D(texture))
                using (var resource = tex11.QueryInterface<SharpDX.DXGI.Resource>()) {
                    sharedHandle = resource.SharedHandle;
                    width = tex11.Description.Width;
                    height = tex11.Description.Height;
                }

                if (sharedHandle == IntPtr.Zero) {
                    throw new InvalidOperationException("Texture should be created with a shared flag");
                }

                EnsureDevice();

                if (_texture == null || sharedHandle != _lastSharedHandle) {
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                    ReleaseResources();

                    var handle = sharedHandle;
                    _texture = new Texture(_device, width, height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default, ref handle);
                    _surface = _texture.GetSurfaceLevel(0);
                    _lastSharedHandle = sharedHandle;
                }

                SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surface.NativePointer);
            } finally {
                Unlock();
            }
        }

        public void Dispose() {
            ReleaseResources();
            _device?.Dispose();
            _device = null;
            _direct3D?.Dispose();
            _direct3D = null;
        }
    }
}
