using DirectXTexNet;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    class PrefilterDiffuseEnvMap
    {
        const string ShaderFileName = "./EnvMapProcessing.hlsl";
        const string ShaderProfile = "cs_5_0";
        const string ShaderPrefilterEntryPoint = "PrefilterDiffuseEnvMapCS";

        const int PrefilteredDiffuseCubemapSize = 64;

        public struct ShaderConstants
        {
            public uint CubeMapInSize;
            public uint CubeMapOutSize;
            public uint SampleCount;
            public float Roughness;
        }

        const int D3D11_1_UAV_SLOT_COUNT = 64;
        const int D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT = 128;
        const int D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT = 14;

        readonly ID3D11UnorderedAccessView[] zeroUav;
        readonly ID3D11ShaderResourceView[] zeroSrvs;
        readonly ID3D11Buffer[] zeroBuffers;

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

        public PrefilterDiffuseEnvMap(ID3D11Device device, ScratchImage cubemaps, uint arraySize, uint cubeMapSize, uint cubemapCount, DXGI_FORMAT format, uint sampleCount)
        {
            this.device = device;
            this.arraySize = arraySize;
            this.cubeMapSize = cubeMapSize;
            this.cubemapCount = cubemapCount;
            this.format = (Format)format;
            this.sampleCount = sampleCount;

            TexMetadata metaData = cubemaps.GetMetadata();
            Debug.Assert(device != null && metaData.IsCubemap() && cubemapCount > 0 && (arraySize % 6) == 0);

            // Upload source cubemaps and create output resources
            Texture2DDescription desc = new()
            {
                Width = (uint)metaData.Width,
                Height = (uint)metaData.Height,
                MipLevels = (uint)metaData.MipLevels,
                ArraySize = arraySize,
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

            desc.Width = desc.Height = PrefilteredDiffuseCubemapSize;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.UnorderedAccess;
            desc.MiscFlags = 0;
            cubemapsOut = device.CreateTexture2D(desc);

            desc.BindFlags = 0;
            desc.Usage = ResourceUsage.Staging;
            desc.CPUAccessFlags = CpuAccessFlags.Read;
            cubemapsCpu = device.CreateTexture2D(desc);

            var shaderCode = Compiler.CompileFromFile(ShaderFileName, ShaderPrefilterEntryPoint, ShaderProfile);
            shaderPrefilter = device.CreateComputeShader(shaderCode.ToArray());

            constantBuffer = CreateConstantBuffer();

            linearSampler = CreateLinearSampler();

            zeroUav = new ID3D11UnorderedAccessView[D3D11_1_UAV_SLOT_COUNT];
            Array.Fill(zeroUav, new ID3D11UnorderedAccessView(IntPtr.Zero));

            zeroSrvs = new ID3D11ShaderResourceView[D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT];
            Array.Fill(zeroSrvs, new ID3D11ShaderResourceView(IntPtr.Zero));

            zeroBuffers = new ID3D11Buffer[D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT];
            Array.Fill(zeroBuffers, new ID3D11Buffer(IntPtr.Zero));
        }
        ID3D11Buffer CreateConstantBuffer()
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
        ID3D11SamplerState CreateLinearSampler()
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

        public bool Run()
        {
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            ShaderConstants constants = new()
            {
                CubeMapInSize = cubeMapSize,
                CubeMapOutSize = PrefilteredDiffuseCubemapSize,
                SampleCount = sampleCount,
            };
            if (!SetConstants(constants))
            {
                return false;
            }

            ResetD3d11Context();

            for (uint i = 0; i < cubemapCount; i++)
            {
                ID3D11ShaderResourceView cubemapInSrv = CreateCubemapSrv(i * 6);
                if (cubemapInSrv == null)
                {
                    return false;
                }

                ID3D11UnorderedAccessView cubemapOutUav = CreateTexture2dUav(i * 6);
                if (cubemapOutUav == null)
                {
                    return false;
                }

                uint blockSize = (PrefilteredDiffuseCubemapSize + 15) >> 4;
                Dispatch(cubemapInSrv, cubemapOutUav, constantBuffer, linearSampler, shaderPrefilter, [blockSize, blockSize, 6]);
            }

            ResetD3d11Context();

            return true;
        }
        bool SetConstants(ShaderConstants constants)
        {
            var ctx = device.ImmediateContext;

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
        void ResetD3d11Context()
        {
            var ctx = device.ImmediateContext;

            ctx.CSSetUnorderedAccessViews(0, zeroUav);
            ctx.CSSetShaderResources(0u, zeroSrvs);
            ctx.CSSetConstantBuffers(0u, new Span<ID3D11Buffer>(zeroBuffers));
        }
        ID3D11ShaderResourceView CreateCubemapSrv(uint firstArraySlice)
        {
            ShaderResourceViewDescription desc = new()
            {
                Format = format,
                ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.TextureCubeArray
            };

            desc.TextureCubeArray.NumCubes = 1;
            desc.TextureCubeArray.First2DArrayFace = firstArraySlice;
            desc.TextureCubeArray.MipLevels = 1;

            return device.CreateShaderResourceView(cubemapsIn, desc);
        }
        ID3D11UnorderedAccessView CreateTexture2dUav(uint firstArraySlice)
        {
            UnorderedAccessViewDescription desc = new()
            {
                Format = format,
                ViewDimension = UnorderedAccessViewDimension.Texture2DArray
            };
            desc.Texture2DArray.ArraySize = 6;
            desc.Texture2DArray.FirstArraySlice = firstArraySlice;
            desc.Texture2DArray.MipSlice = 0;

            return device.CreateUnorderedAccessView(cubemapsOut, desc);
        }
        void Dispatch(ID3D11ShaderResourceView srvArray, ID3D11UnorderedAccessView uavArray, ID3D11Buffer buffersArray, ID3D11SamplerState samplersArray, ID3D11ComputeShader shader, uint[] groupCount)
        {
            var ctx = device.ImmediateContext;

            ctx.CSSetShaderResource(0, srvArray);
            ctx.CSSetUnorderedAccessView(0, uavArray);
            ctx.CSSetConstantBuffer(0, buffersArray);
            ctx.CSSetSampler(0, samplersArray);
            ctx.CSSetShader(shader, null, 0);
            ctx.Dispatch(groupCount[0], groupCount[1], groupCount[2]);
        }

        public bool DownloadCubemaps(ScratchImage result, uint mipLevels)
        {
            var ctx = device.ImmediateContext;

            ctx.CopyResource(cubemapsCpu, cubemapsOut);

            for (uint imgIdx = 0; imgIdx < arraySize; imgIdx++)
            {
                for (uint mip = 0; mip < mipLevels; mip++)
                {
                    uint resIdx = mip + (imgIdx * mipLevels);

                    var hr = ctx.Map(cubemapsCpu, resIdx, MapMode.Read, 0, out var map);
                    if (hr.Failure)
                    {
                        return false;
                    }
                    var img = result.GetImage((int)mip, (int)imgIdx, 0);

                    var src = map.DataPointer;
                    int srcSize = (int)map.RowPitch;
                    BlobStreamWriter dst = new(img.Pixels, img.Height * srcSize);

                    for (uint row = 0; row < img.Height; row++)
                    {
                        dst.Write(src, (int)img.RowPitch);
                        src += srcSize;
                    }

                    ctx.Unmap(cubemapsCpu, resIdx);
                }
            }

            return true;
        }
    }
}
