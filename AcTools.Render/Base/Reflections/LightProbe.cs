using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using AcTools.Render.Base.Utils;
using SharpDX.Direct3D;

namespace AcTools.Render.Base.Reflections {
    public class LightProbe : ReflectionCubemap {
        public LightProbe() : base(32) { }

        protected override int GetMipLevels(int resolution, int minResolution) {
            return 1;
        }

        private float[][] _buffer = { new float[9], new float[9], new float[9] };
        private Vector4[] _values = new Vector4[9];

        public Vector4[] Values => _values;

        protected override void OnCubemapUpdate(DeviceContextHolder holder) {
            base.OnCubemapUpdate(holder);

            var buffer = _buffer;
            ShProjectCubeMap(holder.DeviceContext, 3, CubeTex, buffer[0], buffer[1], buffer[2]);
            for (var i = 0; i < 9; i++) {
                _values[i].X = buffer[0][i];
                _values[i].Y = buffer[1][i];
                _values[i].Z = buffer[2][i];
            }
        }

        private static void ShProjectCubeMap(DeviceContext contextRef, int order, Texture2D cubeMapRef, float[] rOutRef, float[] gOutRef, float[] bOutRef) {
            unsafe {
                fixed (void* r = rOutRef)
                fixed (void* g = gOutRef)
                fixed (void* b = bOutRef) {
                    var result = (D3DX11SHProjectCubeMap(
                            (void*)(contextRef?.NativePointer ?? IntPtr.Zero), order,
                            (void*)(cubeMapRef?.NativePointer ?? IntPtr.Zero), r, g, b));
                    if (result < 0) {
                        throw new SharpDXException(result);
                    }
                }
            }
        }

        [DllImport("d3dx11_43.dll", EntryPoint = "D3DX11SHProjectCubeMap", CallingConvention = CallingConvention.StdCall)]
        private static extern unsafe int D3DX11SHProjectCubeMap(void* arg0, int arg1, void* arg2, void* arg3, void* arg4, void* arg5);
    }
}