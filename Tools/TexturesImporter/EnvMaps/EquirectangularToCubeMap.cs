using DirectXTexNet;
using System;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter.EnvMaps
{
    class EquirectangularToCubeMap : IDisposable
    {
        private readonly ID3D11Device device;
        private readonly uint cubemapSize;
        private readonly uint arraySize;
        private readonly Format format;

        private readonly ID3D11Texture2D cubemaps;
        private readonly ID3D11Texture2D cubemapsCpu;
        private readonly ID3D11ComputeShader shaderCubeMap;
        private readonly ID3D11Buffer constantBuffer;
        private readonly ID3D11SamplerState linearSampler;

        public EquirectangularToCubeMap(ID3D11Device device, int cubemapSize, int arraySize, DXGI_FORMAT format)
        {
            this.device = device;
            this.cubemapSize = (uint)cubemapSize;
            this.arraySize = (uint)arraySize;
            this.format = (Format)format;

            // Create output resources
            Texture2DDescription descTx = new()
            {
                Width = (uint)cubemapSize,
                Height = (uint)cubemapSize,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = (Format)format,
                SampleDescription = new(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess,
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            cubemaps = device.CreateTexture2D(descTx);

            descTx.BindFlags = 0;
            descTx.Usage = ResourceUsage.Staging;
            descTx.CPUAccessFlags = CpuAccessFlags.Read;

            cubemapsCpu = device.CreateTexture2D(descTx);

            shaderCubeMap = EnvMapProcessingShader.GetCubeMapShader(device);

            constantBuffer = EnvMapProcessingShader.CreateConstantBuffer(device);

            linearSampler = EnvMapProcessingShader.CreateLinearSampler(device);
        }
        ~EquirectangularToCubeMap()
        {
            Dispose(false);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                linearSampler?.Dispose();
                constantBuffer?.Dispose();
                shaderCubeMap?.Dispose();
                cubemapsCpu?.Dispose();
                cubemaps?.Dispose();
            }
        }

        public bool Run(Image[] envMaps, bool mirrorCubemap)
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            EnvMapProcessingShader.ShaderConstants constants = new()
            {
                CubeMapOutSize = cubemapSize,
                SampleCount = mirrorCubemap ? 1u : 0u // Misusing sampleCount as a toggle for mirroring the cubemap.
            };
            if (!EnvMapProcessingShader.SetConstants(ctx, constantBuffer, constants))
            {
                return false;
            }

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            for (uint i = 0; i < envMaps.Length; i++)
            {
                uint firstArraySlice = i * 6;

                var src = envMaps[i];

                Texture2DDescription desc = new()
                {
                    Width = (uint)src.Width,
                    Height = (uint)src.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = format,
                    SampleDescription = new(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                SubresourceData data = new()
                {
                    DataPointer = src.Pixels,
                    RowPitch = (uint)src.RowPitch
                };

                using var envMap = device.CreateTexture2D(desc, data);
                using var envMapSrv = device.CreateShaderResourceView(envMap);
                using var cubemapUav = EnvMapProcessingShader.CreateTexture2DUav(device, format, 6, firstArraySlice, 0, cubemaps);

                // Upload source image to GPU
                EnvMapProcessingShader.Dispatch(ctx, envMapSrv, cubemapUav, constantBuffer, linearSampler, shaderCubeMap, cubemapSize);
            }

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            return true;
        }

        public bool Download(out ScratchImage result)
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            result = TexHelper.Instance.InitializeCube((DXGI_FORMAT)format, (int)cubemapSize, (int)cubemapSize, (int)arraySize / 6, 1, CP_FLAGS.NONE);

            return EnvMapProcessingShader.DownloadTexture2D(ctx, cubemaps, cubemapsCpu, arraySize, 1, result);
        }
    }
}
