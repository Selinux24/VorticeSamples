using Engine;
using Engine.Graphics;
using System;

namespace Direct3D12
{
    public class D3D12Graphics : Graphics
    {
        /// <inheritdoc/>
        public override void Shutdown()
        {

        }

        /// <inheritdoc/>
        public override ISurface CreateSurface(Window window)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSurface(int id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void ResizeSurface(int id, uint width, uint height)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RenderSurface(int id, IFrameInfo info)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ICamera CreateCamera(ICameraInitInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveCamera(int id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetCameraParameter<T>(int id, ICameraParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetCameraParameter<T>(int id, ICameraParameters parameter)
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
        public override void RemoveLight(int id, ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetLightParameter<T>(int id, ulong lightSetKey, ILightParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetLightParameter<T>(int id, ulong lightSetKey, ILightParameters parameter)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override int AddSubmesh(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSubmesh(int id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override int AddTexture(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveTexture(int id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override int AddMaterial(IMaterialInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveMaterial(int id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override int AddRenderItem(int entityId, int geometryContentId, int materialCount, int[] materialIds)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveRenderItem(int id)
        {
            throw new NotImplementedException();
        }
    }
}
