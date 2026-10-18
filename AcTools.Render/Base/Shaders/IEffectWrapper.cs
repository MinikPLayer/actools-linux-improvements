using SharpDX.Direct3D11;
using AcTools.Render.Base.Utils;
using SharpDX.Direct3D;

namespace AcTools.Render.Base.Shaders {
    public interface IEffectWrapper : System.IDisposable {
        void Initialize(Device device);
    }
}