using Engine.Graphics;
using Engine.Platform;
using System;

namespace Direct3D12
{
    public class D3D12Graphics : GraphicsBase
    {
        /// <inheritdoc/>
        public override void Shutdown()
        {

        }

        /// <inheritdoc/>
        public override ISurface CreateSurface(PlatformWindow window)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSurface(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void ResizeSurface(uint id, uint width, uint height)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RenderSurface(uint id, IFrameInfo info)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ICamera CreateCamera(ICameraInitInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveCamera(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetCameraParameter<T>(uint id, ICameraParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetCameraParameter<T>(uint id, ICameraParameters parameter)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void CreateLightSet(ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveLightSet(ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override ILight CreateLight(ILightInitInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveLight(uint id, ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetLightParameter<T>(uint id, ulong lightSetKey, ILightParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetLightParameter<T>(uint id, ulong lightSetKey, ILightParameters parameter)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override uint AddSubmesh(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSubmesh(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddTexture(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveTexture(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddMaterial(IMaterialInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveMaterial(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddRenderItem(uint entityId, uint geometryContentId, uint[] materialIds)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveRenderItem(uint id)
        {
            throw new NotImplementedException();
        }
    }
}
