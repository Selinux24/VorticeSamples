using DirectXTexNet;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Utilities;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    static class EnvMapProcessing
    {
        const string ShaderFileName = "./EnvMapProcessing.hlsl";

        const int PrefilteredSpecularCubemapSize = 256;
        const float ThresholdDefault = 0.5f;

        const int D3D11_1_UAV_SLOT_COUNT = 64;
        const int D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT = 128;
        const int D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT = 14;

        static readonly ID3D11UnorderedAccessView[] zeroUav = new ID3D11UnorderedAccessView[D3D11_1_UAV_SLOT_COUNT];
        static readonly ID3D11ShaderResourceView[] zeroSrvs = new ID3D11ShaderResourceView[D3D11_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT];
        static readonly ID3D11Buffer[] zeroBuffers = new ID3D11Buffer[D3D11_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT];

        struct ShaderConstants
        {
            public uint CubeMapInSize;
            public uint CubeMapOutSize;
            public uint SampleCount;
            public float Roughness;
        }

        private static TexHelper Helper => TexHelper.Instance;

        public static bool EquirectangularToCubemapCPU(Image[] envMaps, int envMapCount, int cubemapSize, bool usePrefilterSize, bool mirrorCubemap, out ScratchImage cubeMaps)
        {
            if (usePrefilterSize)
            {
                cubemapSize = PrefilteredSpecularCubemapSize;
            }

            Debug.Assert(envMaps != null && envMapCount > 0);

            // Initialize 1 texture cube for each image.
            ScratchImage workingScratch = Helper.InitializeCube(DXGI_FORMAT.R32G32B32A32_FLOAT, cubemapSize, cubemapSize, envMapCount, 1, CP_FLAGS.NONE);
            if (workingScratch == null)
            {
                cubeMaps = null;
                return false;
            }

            for (int i = 0; i < envMapCount; i++)
            {
                Image envMap = envMaps[i];
                Debug.Assert(Utils.Equal((float)envMap.Width / envMap.Height, 2f));

                // NOTE: all env_maps are equirectangular images with the same size and format.
                //       We already checked for matching size and format in the main import function.
                //       Here we convert each env_map to a linear color space 32-bit float for easier sampling.
                ScratchImage f32EnvMap;
                if (envMaps[0].Format != DXGI_FORMAT.R32G32B32A32_FLOAT)
                {
                    f32EnvMap = workingScratch.Convert(i, DXGI_FORMAT.R32G32B32A32_FLOAT, TEX_FILTER_FLAGS.DEFAULT, ThresholdDefault);
                    if (f32EnvMap == null)
                    {
                        cubeMaps = null;
                        return false;
                    }
                }
                else
                {
                    f32EnvMap = workingScratch.CreateImageCopy(i, true, CP_FLAGS.NONE);
                }

                Debug.Assert(f32EnvMap.GetImageCount() == 1);
                Image[] dstImages = new Image[6];
                for (int j = i * 6; j < 6; j++)
                {
                    dstImages[j] = workingScratch.GetImage(j);
                }
                Image envMapImage = f32EnvMap.GetImage(i);
                bool mirror = mirrorCubemap;

                Thread[] tasks =
                [
                    new(() => { SampleCubeFace(envMapImage, dstImages[0], 0, mirror); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[1], 1, mirror); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[2], 2, mirror); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[3], 3, mirror); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[4], 4, mirror); }),
                ];

                SampleCubeFace(f32EnvMap.GetImage(i), dstImages[5], 5, mirror);

                foreach (var t in tasks)
                {
                    t.Start();
                }

                foreach (var t in tasks)
                {
                    t.Join();
                }
            }

            if (envMaps[0].Format != DXGI_FORMAT.R32G32B32A32_FLOAT)
            {
                cubeMaps = workingScratch.Convert(envMaps[0].Format, TEX_FILTER_FLAGS.DEFAULT, ThresholdDefault);
            }
            else
            {
                cubeMaps = workingScratch;
            }

            return true;
        }
        static void SampleCubeFace(Image envMap, Image cubeFace, int faceIndex, bool mirror)
        {
            Debug.Assert(cubeFace.Width == cubeFace.Height);
            float invWidth = 1f / cubeFace.Width;
            float invHeight = 1f / cubeFace.Height;
            long rowPitch = cubeFace.RowPitch;
            long envWidth = envMap.Width - 1;
            long envHeight = envMap.Height - 1;
            long envRowPitch = envMap.RowPitch;

            // assume both env_map and cube_face are using DXGI_FORMAT_R32G32B32A32_FLOAT
            int bitsPerPixel = Helper.BitsPerPixel(cubeFace.Format);
            BlobStreamWriter blob = new(cubeFace.Pixels, cubeFace.Width * cubeFace.Height * bitsPerPixel);

            for (int y = 0; y < cubeFace.Height; y++)
            {
                float v = (y * invHeight) * 2f - 1f;

                for (int x = 0; x < cubeFace.Width; x++)
                {
                    float u = (x * invWidth) * 2f - 1f;

                    Vector3 sampleDirection = GetSampleDirectionEquirectangular(faceIndex, u, v);
                    Vector2 uv = DirectionToEquirectangular(sampleDirection);
                    Debug.Assert(uv.X >= 0f && uv.X <= 1f && uv.Y >= 0f && uv.Y <= 1f);

                    if (mirror) uv.X = 1f - uv.X;
                    float posX = uv.X * envWidth;
                    float posY = uv.Y * envHeight;

                    // bufff
                    blob.Position = (nint)(rowPitch * y + x * bitsPerPixel);
                    blob.Write(envMap.Pixels + (nint)(envRowPitch * posY + posX * bitsPerPixel), bitsPerPixel);
                }
            }
        }
        static Vector3 GetSampleDirectionEquirectangular(int face, float u, float v)
        {
            Vector3[] directions =
            [
                new( -u,  1f,  -v),   // X+ Left
                new(  u, -1f,  -v),   // X- Right
                new(  v,   u,  1f),   // Y+ Bottom
                new( -v,   u, -1f),   // Y- Top
                new( 1f,   u,  -v),   // Z+ Front
                new(-1f,  -u,  -v),   // Z- Back
            ];

            var dir = directions[face];
            return Vector3.Normalize(dir);
        }
        static Vector2 DirectionToEquirectangular(Vector3 dir)
        {
            float phi = MathF.Atan2(dir.Y, dir.X);
            float theta = MathF.Acos(dir.Z);
            float s = phi * (1f / MathF.PI * 2f) + 0.5f;
            float t = theta * (1f / MathF.PI);

            return new(s, t);
        }

        public static bool EquirectangularToCubemapGPU(ID3D11Device device, Image[] envMaps, int envMapCount, int cubemapSize, bool usePrefilterSize, bool mirrorCubemap, out ScratchImage cubeMaps)
        {
            cubeMaps = null;

            if (usePrefilterSize)
            {
                cubemapSize = PrefilteredSpecularCubemapSize;
            }

            Debug.Assert(envMaps != null && envMapCount > 0);
            Format format = (Format)envMaps[0].Format;
            int arraySize = envMapCount * 6;
            var ctx = device.ImmediateContext;
            Debug.Assert(ctx != null);

            // Create output resources
            Texture2DDescription descTx = new()
            {
                Width = (uint)cubemapSize,
                Height = (uint)cubemapSize,
                MipLevels = 1,
                ArraySize = (uint)arraySize,
                Format = format,
                SampleDescription = new(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess,
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            var cubemaps = device.CreateTexture2D(descTx);
            if (cubemaps == null)
            {
                return false;
            }

            descTx.BindFlags = 0;
            descTx.Usage = ResourceUsage.Staging;
            descTx.CPUAccessFlags = CpuAccessFlags.Read;

            var cubemapsCpu = device.CreateTexture2D(descTx);
            if (cubemapsCpu == null)
            {
                return false;
            }

            var shaderCode = Compiler.CompileFromFile(ShaderFileName, "EquirectangularToCubeMapCS", "cs_5_0");
            var shader = device.CreateComputeShader(shaderCode.ToArray());
            if (shader == null)
            {
                return false;
            }

            var constantBuffer = CreateConstantBuffer(device);
            if (constantBuffer == null)
            {
                return false;
            }

            ShaderConstants constants = new()
            {
                CubeMapOutSize = (uint)cubemapSize,
                SampleCount = mirrorCubemap ? 1u : 0u // Misusing sampleCount as a toggle for mirroring the cubemap.
            };
            if (!SetConstants(ctx, constantBuffer, constants))
            {
                return false;
            }

            var linearSampler = CreateLinearSampler(device);
            if (linearSampler == null)
            {
                return false;
            }

            ResetD3d11Context(ctx);

            for (int i = 0; i < envMapCount; i++)
            {
                var cubemapUav = CreateTexture2dUav(device, format, 6, (uint)i * 6, 0, cubemaps);
                if (cubemapUav == null)
                {
                    return false;
                }

                var src = envMaps[i];

                // Upload source image to GPU
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

                var envMap = device.CreateTexture2D(desc, data);
                if (envMap == null)
                {
                    return false;
                }

                var envMapSrv = device.CreateShaderResourceView(envMap);
                if (envMapSrv == null)
                {
                    return false;
                }

                uint blockSize = (uint)(cubemapSize + 15) >> 4;

                Dispatch(ctx, envMapSrv, cubemapUav, constantBuffer, linearSampler, shader, [blockSize, blockSize, 6]);
            }

            ResetD3d11Context(ctx);

            return DownloadTexture2d(ctx, (uint)cubemapSize, (uint)cubemapSize, (uint)arraySize, 1, (DXGI_FORMAT)format, true, cubemaps, cubemapsCpu, out cubeMaps);
        }
        static ID3D11Buffer CreateConstantBuffer(ID3D11Device device)
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
        static bool SetConstants(ID3D11DeviceContext ctx, ID3D11Buffer constantBuffer, ShaderConstants constants)
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
        static ID3D11SamplerState CreateLinearSampler(ID3D11Device device)
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
        static void ResetD3d11Context(ID3D11DeviceContext ctx)
        {
            ctx.CSSetUnorderedAccessViews(0, zeroUav);
            ctx.CSSetShaderResources(0u, zeroSrvs);
            ctx.CSSetConstantBuffers(0u, new Span<ID3D11Buffer>(zeroBuffers));
        }
        static ID3D11UnorderedAccessView CreateTexture2dUav(ID3D11Device device, Format format, uint arraySize, uint firstArraySlice, uint mipSlice, ID3D11Resource texture)
        {
            UnorderedAccessViewDescription desc = new()
            {
                Format = format,
                ViewDimension = UnorderedAccessViewDimension.Texture2DArray
            };
            desc.Texture2DArray.ArraySize = arraySize;
            desc.Texture2DArray.FirstArraySlice = firstArraySlice;
            desc.Texture2DArray.MipSlice = mipSlice;

            return device.CreateUnorderedAccessView(texture, desc);
        }
        static void Dispatch(ID3D11DeviceContext ctx, ID3D11ShaderResourceView srvArray, ID3D11UnorderedAccessView uavArray, ID3D11Buffer buffersArray, ID3D11SamplerState samplersArray, ID3D11ComputeShader shader, uint[] groupCount)
        {
            ctx.CSSetShaderResource(0, srvArray);
            ctx.CSSetUnorderedAccessView(0, uavArray);
            ctx.CSSetConstantBuffer(0, buffersArray);
            ctx.CSSetSampler(0, samplersArray);
            ctx.CSSetShader(shader, null, 0);
            ctx.Dispatch(groupCount[0], groupCount[1], groupCount[2]);
        }
        static bool DownloadTexture2d(ID3D11DeviceContext ctx, uint width, uint height, uint arraySize, uint mipLevels, DXGI_FORMAT format, bool isCubemap, ID3D11Texture2D gpuResource, ID3D11Texture2D cpuResource, out ScratchImage result)
        {
            ctx.CopyResource(cpuResource, gpuResource);

            result = isCubemap ?
               Helper.InitializeCube(format, (int)width, (int)height, (int)arraySize / 6, (int)mipLevels, CP_FLAGS.NONE) :
               Helper.Initialize2D(format, (int)width, (int)height, (int)arraySize, (int)mipLevels, CP_FLAGS.NONE);

            if (result == null)
            {
                return false;
            }

            for (uint imgIdx = 0; imgIdx < arraySize; imgIdx++)
            {
                for (uint mip = 0; mip < mipLevels; mip++)
                {
                    uint resourceIdx = mip + (imgIdx * mipLevels);
                    var hr = ctx.Map(cpuResource, resourceIdx, MapMode.Read, 0, out var mappedResource);
                    if (hr.Failure)
                    {
                        result.Dispose();
                        return false;
                    }

                    var img = result.GetImage((int)mip, (int)imgIdx, 0);
                    var src = mappedResource.DataPointer;
                    int srcSize = (int)mappedResource.RowPitch;
                    var dst = img.Pixels;

                    BlobStreamWriter blob = new(dst, srcSize * img.Height);

                    for (uint row = 0; row < img.Height; row++)
                    {
                        blob.Write(src, srcSize);
                        src += srcSize;
                    }

                    ctx.Unmap(cpuResource, resourceIdx);
                }
            }

            return true;
        }
    }
}
