using System;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.Objects;
using JetBrains.Annotations;
using SharpDX;
using SharpDX.Direct3D11;
using AcTools.Render.Base.Utils;
using SharpDX.Direct3D;

namespace AcTools.Render.Base.Materials {
    public interface IRenderableMaterial : IDisposable {
        void EnsureInitialized(IDeviceContextHolder contextHolder);
        
        void Refresh(IDeviceContextHolder contextHolder);

        bool Prepare(IDeviceContextHolder contextHolder, SpecialRenderMode mode);

        void SetMatrices(Matrix objectTransform, ICamera camera);

        void Draw(IDeviceContextHolder contextHolder, int indices, SpecialRenderMode mode);

        bool IsBlending { get; }

        [CanBeNull]
        string Name { get; }
    }

    public interface ISkinnedMaterial : IRenderableMaterial {
        void SetBones(Matrix[] bones);
    }

    public interface IAmbientShadowMaterial : IRenderableMaterial {
        [CanBeNull]
        ShaderResourceView GetView(IDeviceContextHolder contextHolder);
    }
}
