using Direct3D12.Content;
using Direct3D12.Lights;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using System;

namespace Direct3D12
{
    /// <summary>
    /// Graphics platform proxy.
    /// </summary>
    class D3D12GraphicsPlatform : IGraphicsPlatform
    {
        /// <inheritdoc/>
        public bool Initialize()
        {
            return D3D12Graphics.Initialize();
        }
        /// <inheritdoc/>
        public void Shutdown()
        {
            D3D12Graphics.Shutdown();
        }
        /// <inheritdoc/>
        public string GetEngineShaderPath()
        {
            return D3D12Graphics.GetEngineShaderPath();
        }

        /// <inheritdoc/>
        public Surface CreateSurface(Window window)
        {
            return D3D12Graphics.CreateSurface(window);
        }
        /// <inheritdoc/>
        public void RemoveSurface(uint id)
        {
            D3D12Graphics.RemoveSurface(id);
        }
        /// <inheritdoc/>
        public void ResizeSurface(uint id, uint width, uint height)
        {
            D3D12Graphics.ResizeSurface(id);
        }
        /// <inheritdoc/>
        public uint GetSurfaceWidth(uint id)
        {
            return D3D12Graphics.GetSurfaceWidth(id);
        }
        /// <inheritdoc/>
        public uint GetSurfaceHeight(uint id)
        {
            return D3D12Graphics.GetSurfaceHeight(id);
        }
        /// <inheritdoc/>
        public void RenderSurface(uint id, FrameInfo info)
        {
            D3D12Graphics.RenderSurface(id, info);
        }

        /// <inheritdoc/>
        public Light CreateLight(LightInitInfo info)
        {
            return D3D12Light.Create(info);
        }
        /// <inheritdoc/>
        public void RemoveLight(uint id, ulong lightSetKey)
        {
            D3D12Light.Remove(id, lightSetKey);
        }
        /// <inheritdoc/>
        public void SetParameter<T>(uint id, ulong lightSetKey, LightParametersTypes parameter, T value) where T : unmanaged
        {
            D3D12Light.SetParameter(id, lightSetKey, parameter, value);
        }
        /// <inheritdoc/>
        public void GetParameter<T>(uint id, ulong lightSetKey, LightParametersTypes parameter, out T value) where T : unmanaged
        {
            D3D12Light.GetParameter(id, lightSetKey, parameter, out value);
        }

        /// <inheritdoc/>
        public Camera CreateCamera(CameraInitInfo info)
        {
            return D3D12Camera.Create(info);
        }
        /// <inheritdoc/>
        public void RemoveCamera(uint id)
        {
            D3D12Camera.Remove(id);
        }
        /// <inheritdoc/>
        public void SetParameter<T>(uint id, CameraParameters parameter, T value) where T : unmanaged
        {
            D3D12Camera.SetParameter(id, parameter, value);
        }
        /// <inheritdoc/>
        public void GetParameter<T>(uint id, CameraParameters parameter, out T value) where T : unmanaged
        {
            D3D12Camera.GetParameter(id, parameter, out value);
        }

        /// <inheritdoc/>
        public uint AddSubmesh(ref IntPtr data)
        {
            return Submesh.Add(ref data);
        }
        /// <inheritdoc/>
        public void RemoveSubmesh(uint id)
        {
            Submesh.Remove(id);
        }
        /// <inheritdoc/>
        public uint AddMaterial(MaterialInitInfo data)
        {
            return Material.Add(data);
        }
        /// <inheritdoc/>
        public void RemoveMaterial(uint id)
        {
            Material.Remove(id);
        }
        /// <inheritdoc/>
        public uint AddRenderItem(uint entityId, uint geometryContentId, uint[] materialIds)
        {
            return RenderItem.Add(entityId, geometryContentId, materialIds);
        }
        /// <inheritdoc/>
        public void RemoveRenderItem(uint id)
        {
            RenderItem.Remove(id);
        }
    }
}
