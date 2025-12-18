using DirectXTexNet;
using System;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    static class EnvMapProcessingShader
    {
        const string ShaderFileName = "./EnvMapProcessing.hlsl";
        const string ShaderProfile = "cs_5_0";
        const string ShaderCubeMapEntryPoint = "EquirectangularToCubeMapCS";
        const string ShaderPrefilterDiffuseEntryPoint = "PrefilterDiffuseEnvMapCS";
        const string ShaderPrefilterSpecularEntryPoint = "PrefilterSpecularEnvMapCS";
        const string ShaderBrdfIntegrationLutEntryPoint = "ComputeBrdfIntegrationLutCS";

        const int D3D11_1_UAV_SLOT_COUNT = 64;
        const int D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT = 128;
        const int D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT = 14;

        static ID3D11UnorderedAccessView[] zeroUav;
        static ID3D11ShaderResourceView[] zeroSrvs;
        static ID3D11Buffer[] zeroBuffers;

        public struct ShaderConstants
        {
            public uint CubeMapInSize;
            public uint CubeMapOutSize;
            public uint SampleCount;
            public float Roughness;
        }

        public static ID3D11ComputeShader GetCubeMapShader(ID3D11Device device)
        {
            return CreateComputeShader(device, ShaderCubeMapEntryPoint);
        }
        public static ID3D11ComputeShader GetPrefilterDiffuseShader(ID3D11Device device)
        {
            return CreateComputeShader(device, ShaderPrefilterDiffuseEntryPoint);
        }
        public static ID3D11ComputeShader GetPrefilterSpecularShader(ID3D11Device device)
        {
            return CreateComputeShader(device, ShaderPrefilterSpecularEntryPoint);
        }
        public static ID3D11ComputeShader GetBrdfIntegrationLutShader(ID3D11Device device)
        {
            return CreateComputeShader(device, ShaderBrdfIntegrationLutEntryPoint);
        }
        static ID3D11ComputeShader CreateComputeShader(ID3D11Device device, string entryPoint)
        {
            var shaderCode = Compiler.CompileFromFile(ShaderFileName, entryPoint, ShaderProfile);
            return device.CreateComputeShader(shaderCode.ToArray());
        }
        public static ID3D11Buffer CreateConstantBuffer(ID3D11Device device)
        {
            BufferDescription desc = new()
            {
                ByteWidth = (uint)Marshal.SizeOf<ShaderConstants>(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
                MiscFlags = 0,
                StructureByteStride = 0
            };

            return device.CreateBuffer(desc);
        }
        public static ID3D11SamplerState CreateLinearSampler(ID3D11Device device)
        {
            SamplerDescription desc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunc = ComparisonFunction.Never,
                MinLOD = 0,
                MaxLOD = float.MaxValue
            };

            return device.CreateSamplerState(desc);
        }

        public static ID3D11ShaderResourceView CreateCubemapSrv(ID3D11Device device, Format format, ID3D11Texture2D cubemap, uint firstArraySlice, uint mipLevels = 1)
        {
            ShaderResourceViewDescription desc = new()
            {
                Format = format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.TextureCubeArray
            };

            desc.TextureCubeArray.NumCubes = 1;
            desc.TextureCubeArray.First2DArrayFace = firstArraySlice;
            desc.TextureCubeArray.MipLevels = mipLevels;

            return device.CreateShaderResourceView(cubemap, desc);
        }
        public static ID3D11UnorderedAccessView CreateTexture2DUav(ID3D11Device device, Format format, uint arraySize, uint firstArraySlice, uint mipSlice, ID3D11Texture2D cubemap)
        {
            UnorderedAccessViewDescription desc = new()
            {
                Format = format,
                ViewDimension = UnorderedAccessViewDimension.Texture2DArray
            };
            desc.Texture2DArray.ArraySize = arraySize;
            desc.Texture2DArray.FirstArraySlice = firstArraySlice;
            desc.Texture2DArray.MipSlice = mipSlice;

            return device.CreateUnorderedAccessView(cubemap, desc);
        }

        public static bool SetConstants(ID3D11DeviceContext ctx, ID3D11Buffer constantBuffer, ShaderConstants constants)
        {
            var hr = ctx.Map(constantBuffer, 0, MapMode.WriteDiscard, 0, out var mappedBuffer);
            if (hr.Failure || mappedBuffer.DataPointer == IntPtr.Zero)
            {
                return false;
            }

            BlobStreamWriter blob = new(mappedBuffer.DataPointer, (int)mappedBuffer.RowPitch);
            blob.Write(constants);

            ctx.Unmap(constantBuffer, 0);
            return true;
        }
        public static void ResetD3d11Context(ID3D11DeviceContext ctx)
        {
            if (zeroUav == null)
            {
                zeroUav = new ID3D11UnorderedAccessView[D3D11_1_UAV_SLOT_COUNT];
                Array.Fill(zeroUav, new ID3D11UnorderedAccessView(IntPtr.Zero));
            }
            if (zeroSrvs == null)
            {
                zeroSrvs = new ID3D11ShaderResourceView[D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT];
                Array.Fill(zeroSrvs, new ID3D11ShaderResourceView(IntPtr.Zero));
            }
            if (zeroBuffers == null)
            {
                zeroBuffers = new ID3D11Buffer[D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT];
                Array.Fill(zeroBuffers, new ID3D11Buffer(IntPtr.Zero));
            }

            ctx.CSSetUnorderedAccessViews(0, zeroUav);
            ctx.CSSetShaderResources(0u, zeroSrvs);
            ctx.CSSetConstantBuffers(0u, new Span<ID3D11Buffer>(zeroBuffers));
        }
        public static void Dispatch(ID3D11DeviceContext ctx, ID3D11ShaderResourceView srvArray, ID3D11UnorderedAccessView uavArray, ID3D11Buffer buffersArray, ID3D11SamplerState samplersArray, ID3D11ComputeShader shader, uint size)
        {
            uint blockSize = (size + 15) >> 4;

            ctx.CSSetShaderResource(0, srvArray);
            ctx.CSSetUnorderedAccessView(0, uavArray);
            ctx.CSSetConstantBuffer(0, buffersArray);
            ctx.CSSetSampler(0, samplersArray);
            ctx.CSSetShader(shader, null, 0);
            ctx.Dispatch(blockSize, blockSize, 6);
        }

        public static bool DownloadTexture2D(ID3D11DeviceContext ctx, ID3D11Texture2D gpuResource, ID3D11Texture2D cpuResource, uint arraySize, uint mipLevels, ScratchImage result)
        {
            ctx.CopyResource(cpuResource, gpuResource);

            for (uint imgIdx = 0; imgIdx < arraySize; imgIdx++)
            {
                for (uint mip = 0; mip < mipLevels; mip++)
                {
                    uint resIdx = mip + (imgIdx * mipLevels);

                    var hr = ctx.Map(cpuResource, resIdx, MapMode.Read, 0, out var map);
                    if (hr.Failure)
                    {
                        return false;
                    }
                    var img = result.GetImage((int)mip, (int)imgIdx, 0);

                    var src = map.DataPointer;
                    BlobStreamWriter dst = new(img.Pixels, (int)img.SlicePitch);

                    for (uint row = 0; row < img.Height; row++)
                    {
                        dst.Write(src, (int)img.RowPitch);
                        src += (int)map.RowPitch;
                    }

                    ctx.Unmap(cpuResource, resIdx);
                }
            }

            return true;
        }
    }
}
