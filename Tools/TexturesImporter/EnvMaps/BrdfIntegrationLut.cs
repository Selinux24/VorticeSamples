using DirectXTexNet;
using System;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter.EnvMaps
{
    class BrdfIntegrationLut : IDisposable
    {
        const uint BrdfIntegrationLutSize = 256;
        const Format BrdfIntegrationLutFormat = Format.R16G16_Float;

        private readonly ID3D11Device device;
        private readonly uint sampleCount;

        private readonly ID3D11Texture2D output;
        private readonly ID3D11Texture2D outputCpu;
        private readonly ID3D11ComputeShader shader;
        private readonly ID3D11Buffer constantBuffer;
        private readonly ID3D11SamplerState linearSampler;

        public BrdfIntegrationLut(ID3D11Device device, int sampleCount)
        {
            this.device = device;
            this.sampleCount = (uint)sampleCount;

            // Upload source cubemaps and create output resources
            Texture2DDescription desc = new()
            {
                Width = BrdfIntegrationLutSize,
                Height = BrdfIntegrationLutSize,
                MipLevels = 1,
                ArraySize = 1,
                Format = BrdfIntegrationLutFormat,
                SampleDescription = new(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };
            output = device.CreateTexture2D(desc);

            desc.BindFlags = BindFlags.None;
            desc.Usage = ResourceUsage.Staging;
            desc.CPUAccessFlags = CpuAccessFlags.Read;
            outputCpu = device.CreateTexture2D(desc);

            shader = EnvMapProcessingShader.GetBrdfIntegrationLutShader(device);

            constantBuffer = EnvMapProcessingShader.CreateConstantBuffer(device);

            linearSampler = EnvMapProcessingShader.CreateLinearSampler(device);
        }
        ~BrdfIntegrationLut()
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
                shader?.Dispose();
                outputCpu?.Dispose();
                output?.Dispose();
            }
        }

        public bool Run()
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            EnvMapProcessingShader.ShaderConstants constants = new()
            {
                CubeMapInSize = BrdfIntegrationLutSize,
                SampleCount = sampleCount,
            };
            if (!EnvMapProcessingShader.SetConstants(ctx, constantBuffer, constants))
            {
                return false;
            }

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            using var outputUav = EnvMapProcessingShader.CreateTexture2DUav(device, BrdfIntegrationLutFormat, 1, 0, 0, output);
            if (outputUav == null)
            {
                return false;
            }

            EnvMapProcessingShader.Dispatch(ctx, null, outputUav, constantBuffer, linearSampler, shader, BrdfIntegrationLutSize);

            EnvMapProcessingShader.ResetD3d11Context(ctx);

            return true;
        }

        public bool Download(out ScratchImage result)
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            result = TexHelper.Instance.Initialize2D((DXGI_FORMAT)BrdfIntegrationLutFormat, (int)BrdfIntegrationLutSize, (int)BrdfIntegrationLutSize, 1, 1, CP_FLAGS.NONE);

            return EnvMapProcessingShader.DownloadTexture2D(ctx, output, outputCpu, 1, 1, result);
        }
    }
}
