using DirectXTexNet;
using System;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    class PrefilterDiffuseEnvMap : IDisposable
    {
        private readonly ID3D11Device device;
        private readonly uint sampleCount;
        private readonly uint arraySize;
        private readonly uint cubeMapSize;
        private readonly uint cubemapCount;
        private readonly Format format;

        private readonly ID3D11Texture2D cubemapsIn;
        private readonly ID3D11Texture2D cubemapsOut;
        private readonly ID3D11Texture2D cubemapsCpu;
        private readonly ID3D11ComputeShader shaderPrefilter;
        private readonly ID3D11Buffer constantBuffer;
        private readonly ID3D11SamplerState linearSampler;

        public PrefilterDiffuseEnvMap(ID3D11Device device, ScratchImage cubemaps, int arraySize, int cubeMapSize, int cubemapCount, DXGI_FORMAT format, int sampleCount)
        {
            this.device = device;
            this.arraySize = (uint)arraySize;
            this.cubeMapSize = (uint)cubeMapSize;
            this.cubemapCount = (uint)cubemapCount;
            this.format = (Format)format;
            this.sampleCount = (uint)sampleCount;

            TexMetadata metaData = cubemaps.GetMetadata();
            Debug.Assert(device != null && metaData.IsCubemap() && cubemapCount > 0 && (arraySize % 6) == 0);

            // Upload source cubemaps and create output resources
            Texture2DDescription desc = new()
            {
                Width = (uint)metaData.Width,
                Height = (uint)metaData.Height,
                MipLevels = (uint)metaData.MipLevels,
                ArraySize = (uint)arraySize,
                Format = (Format)format,
                SampleDescription = new(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.TextureCube
            };

            uint imageCount = (uint)cubemaps.GetImageCount();
            SubresourceData[] inputData = new SubresourceData[imageCount];
            for (uint i = 0; i < imageCount; i++)
            {
                var img = cubemaps.GetImage((int)i);

                inputData[i] = new SubresourceData
                {
                    DataPointer = img.Pixels,
                    RowPitch = (uint)img.RowPitch,
                    SlicePitch = (uint)img.SlicePitch
                };
            }
            cubemapsIn = device.CreateTexture2D(desc, inputData);

            desc.Width = desc.Height = EnvMapProcessing.PrefilteredDiffuseCubemapSize;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.UnorderedAccess;
            desc.MiscFlags = 0;
            cubemapsOut = device.CreateTexture2D(desc);

            desc.BindFlags = 0;
            desc.Usage = ResourceUsage.Staging;
            desc.CPUAccessFlags = CpuAccessFlags.Read;
            cubemapsCpu = device.CreateTexture2D(desc);

            shaderPrefilter = EnvMapProcessingShader.GetPrefilterShader(device);

            constantBuffer = EnvMapProcessingShader.CreateConstantBuffer(device);

            linearSampler = EnvMapProcessingShader.CreateLinearSampler(device);
        }
        ~PrefilterDiffuseEnvMap()
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
                shaderPrefilter?.Dispose();
                cubemapsCpu?.Dispose();
                cubemapsOut?.Dispose();
                cubemapsIn?.Dispose();
            }
        }

        public bool Run()
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            EnvMapProcessingShader.ShaderConstants constants = new()
            {
                CubeMapInSize = cubeMapSize,
                CubeMapOutSize = EnvMapProcessing.PrefilteredDiffuseCubemapSize,
                SampleCount = sampleCount,
            };
            if (!EnvMapProcessingShader.SetConstants(ctx, constantBuffer, constants))
            {
                return false;
            }

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            for (uint i = 0; i < cubemapCount; i++)
            {
                using var cubemapInSrv = EnvMapProcessingShader.CreateCubemapSrv(device, format, cubemapsIn, i * 6);
                if (cubemapInSrv == null)
                {
                    return false;
                }

                using var cubemapOutUav = EnvMapProcessingShader.CreateTexture2DUav(device, format, cubemapsOut, i * 6);
                if (cubemapOutUav == null)
                {
                    return false;
                }

                EnvMapProcessingShader.Dispatch(ctx, cubemapInSrv, cubemapOutUav, constantBuffer, linearSampler, shaderPrefilter, EnvMapProcessing.PrefilteredDiffuseCubemapSize);
            }

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            return true;
        }

        public bool DownloadCubemaps(ScratchImage result, int mipLevels)
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            return EnvMapProcessingShader.DownloadCubemaps(ctx, cubemapsCpu, cubemapsOut, arraySize, (uint)mipLevels, result);
        }
    }
}
