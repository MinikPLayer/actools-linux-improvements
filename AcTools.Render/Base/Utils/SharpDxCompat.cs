using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using Resource = SharpDX.Direct3D11.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using SharpDX.Mathematics.Interop;

namespace AcTools.Render.Base.Utils {
    /// <summary>
    /// A few extension methods to keep the code written for SlimDX working with SharpDX.
    /// </summary>
    public static class SharpDxCompat {
        private static RawViewportF ToRaw(ViewportF v) {
            return new RawViewportF {
                X = v.X, Y = v.Y, Width = v.Width, Height = v.Height, MinDepth = v.MinDepth, MaxDepth = v.MaxDepth
            };
        }

        private static ViewportF FromRaw(RawViewportF v) {
            return new ViewportF(v.X, v.Y, v.Width, v.Height, v.MinDepth, v.MaxDepth);
        }

        public static void SetViewports(this RasterizerStage stage, ViewportF viewport) {
            stage.SetViewports(new[] { ToRaw(viewport) });
        }

        public static void SetViewports(this RasterizerStage stage, ViewportF[] viewports) {
            var raw = new RawViewportF[viewports.Length];
            for (var i = 0; i < raw.Length; i++) {
                raw[i] = ToRaw(viewports[i]);
            }

            stage.SetViewports(raw);
        }

        public static ViewportF[] GetViewportsF(this RasterizerStage stage) {
            var raw = stage.GetViewports<RawViewportF>();
            var result = new ViewportF[raw.Length];
            for (var i = 0; i < raw.Length; i++) {
                result[i] = FromRaw(raw[i]);
            }

            return result;
        }

        public static void ClearRenderTargetView(this DeviceContext context, RenderTargetView view, System.Drawing.Color color) {
            context.ClearRenderTargetView(view, new RawColor4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
        }

        public static Color4 ToColor4(this System.Drawing.Color color) {
            return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static int ToArgb(this Color4 color) {
            return (((int)(color.Alpha * 255f + 0.5f).Clamp(0, 255)) << 24) | (((int)(color.Red * 255f + 0.5f).Clamp(0, 255)) << 16)
                    | (((int)(color.Green * 255f + 0.5f).Clamp(0, 255)) << 8) | (int)(color.Blue * 255f + 0.5f).Clamp(0, 255);
        }

        private static float Clamp(this float v, float min, float max) {
            return v < min ? min : v > max ? max : v;
        }

        /// <summary>
        /// Texture description of a resource behind a view (SharpDX returns a generic Resource object, so it’s not possible to cast it
        /// to Texture2D directly like it was with SlimDX).
        /// </summary>
        public static Texture2DDescription? GetTexture2DDescription(this ResourceView view) {
            using (var resource = view.Resource) {
                using (var texture = resource.QueryInterfaceOrNull<Texture2D>()) {
                    return texture?.Description;
                }
            }
        }

        public struct MappedBox {
            public DataStream Data;
            public IntPtr DataPointer;
            public int RowPitch;
            public int SlicePitch;
        }

        /// <summary>
        /// Same as MapSubresource() from SlimDX: returns a structure with a stream.
        /// </summary>
        public static MappedBox MapSubresourceCompat(this DeviceContext context, Resource resource, int subresource, MapMode mode, MapFlags flags) {
            var box = context.MapSubresource(resource, subresource, mode, flags, out var stream);
            return new MappedBox { Data = stream, DataPointer = box.DataPointer, RowPitch = box.RowPitch, SlicePitch = box.SlicePitch };
        }

        public static MappedBox MapSubresourceCompat(this DeviceContext context, Resource resource, MapMode mode, MapFlags flags) {
            return MapSubresourceCompat(context, resource, 0, mode, flags);
        }

        public static bool RayIntersects(Ray ray, Plane plane, out float distance) {
            return Collision.RayIntersectsPlane(ref ray, ref plane, out distance);
        }

        public static bool RayIntersects(Ray ray, BoundingBox box, out float distance) {
            return Collision.RayIntersectsBox(ref ray, ref box, out distance);
        }

        public static bool RayIntersects(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance) {
            return Collision.RayIntersectsTriangle(ref ray, ref a, ref b, ref c, out distance);
        }

        public static PlaneIntersectionType PlaneIntersects(Plane plane, BoundingBox box) {
            return Collision.PlaneIntersectsBox(ref plane, ref box);
        }

        public static ContainmentType BoxContains(BoundingBox box, Vector3 point) {
            return Collision.BoxContainsPoint(ref box, ref point);
        }

        /// <summary>
        /// D3DXMatrixReflect.
        /// </summary>
        public static Matrix ReflectionMatrix(Plane plane) {
            plane.Normalize();
            var a = plane.Normal.X;
            var b = plane.Normal.Y;
            var c = plane.Normal.Z;
            var d = plane.D;
            return new Matrix(
                    -2f * a * a + 1f, -2f * b * a, -2f * c * a, 0f,
                    -2f * a * b, -2f * b * b + 1f, -2f * c * b, 0f,
                    -2f * a * c, -2f * b * c, -2f * c * c + 1f, 0f,
                    -2f * a * d, -2f * b * d, -2f * c * d, 1f);
        }
    }
}
