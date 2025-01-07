using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
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
        public ISurface CreateSurface(PlatformWindow window)
        {
            return D3D12Graphics.CreateSurface(window);
        }
        /// <inheritdoc/>
        public void RemoveSurface(uint id)
        {
            D3D12Graphics.RemoveSurface(id);
        }
        /// <inheritdoc/>
        public void ResizeSurface(uint id, int width, int height)
        {
            D3D12Graphics.ResizeSurface(id, width, height);
        }
        /// <inheritdoc/>
        public int GetSurfaceWidth(uint id)
        {
            return D3D12Graphics.GetSurfaceWidth(id);
        }
        /// <inheritdoc/>
        public int GetSurfaceHeight(uint id)
        {
            return D3D12Graphics.GetSurfaceHeight(id);
        }
        /// <inheritdoc/>
        public void RenderSurface(uint id)
        {
            D3D12Graphics.RenderSurface(id);
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
        public void SetParameter(uint id, CameraParameters parameter, IntPtr data, int size)
        {
            D3D12Camera.SetParameter(id, parameter, data, size);
        }
        /// <inheritdoc/>
        public void GetParameter(uint id, CameraParameters parameter, IntPtr data, int size)
        {
            D3D12Camera.GetParameter(id, parameter, data, size);
        }

        /// <inheritdoc/>
        public uint AddSubmesh(ref IntPtr data)
        {
            return D3D12Content.AddSubmesh(ref data);
        }
        /// <inheritdoc/>
        public void RemoveSubmesh(uint id)
        {
            D3D12Content.Remove(id);
        }
    }
}
