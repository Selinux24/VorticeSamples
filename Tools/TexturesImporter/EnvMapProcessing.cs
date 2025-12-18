using DirectXTexNet;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using TexturesImporter.Processors;
using Utilities;
using Vortice.Direct3D11;

namespace TexturesImporter
{
    static class EnvMapProcessing
    {
        const float ThresholdDefault = 0.5f;

        public const int PrefilteredDiffuseCubemapSize = 32;
        public const int PrefilteredSpecularCubemapSize = 256;
        public const int RoughnessMipLevels = 6;
        public const int BrdfIntegrationLutSize = 256;

        private static TexHelper Helper => TexHelper.Instance;

        public static ScratchImage EquirectangularToCubemapCPU(Image[] envMaps, int cubemapSize, bool usePrefilterSize, bool mirrorCubemap)
        {
            Debug.Assert(envMaps != null && envMaps.Length > 0);

            if (usePrefilterSize)
            {
                cubemapSize = PrefilteredSpecularCubemapSize;
            }

            // Initialize 1 texture cube for each image.
            var workingScratch = Helper.InitializeCube(DXGI_FORMAT.R32G32B32A32_FLOAT, cubemapSize, cubemapSize, envMaps.Length, 1, CP_FLAGS.NONE);

            for (int i = 0; i < envMaps.Length; i++)
            {
                var envMap = envMaps[i];
                Debug.Assert(Utils.Equal((float)envMap.Width / envMap.Height, 2f));

                // NOTE: all env_maps are equirectangular images with the same size and format.
                //       We already checked for matching size and format in the main import function.
                //       Here we convert each env_map to a linear color space 32-bit float for easier sampling.
                bool is32 = envMaps[0].Format == DXGI_FORMAT.R32G32B32A32_FLOAT;
                using var f32EnvMap = is32 ?
                    workingScratch.CreateImageCopy(i, true, CP_FLAGS.NONE) :
                    workingScratch.Convert(i, DXGI_FORMAT.R32G32B32A32_FLOAT, TEX_FILTER_FLAGS.DEFAULT, ThresholdDefault);

                Debug.Assert(f32EnvMap.GetImageCount() == 1);
                var envMapImage = f32EnvMap.GetImage(i);
                var dstImages = new Image[6];
                for (int j = i * 6; j < 6; j++)
                {
                    dstImages[j] = workingScratch.GetImage(j);
                }

                Thread[] tasks =
                [
                    new(() => { SampleCubeFace(envMapImage, dstImages[0], 0, mirrorCubemap); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[1], 1, mirrorCubemap); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[2], 2, mirrorCubemap); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[3], 3, mirrorCubemap); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[4], 4, mirrorCubemap); }),
                    new(() => { SampleCubeFace(envMapImage, dstImages[5], 5, mirrorCubemap); }),
                ];

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
                var result = workingScratch.Convert(envMaps[0].Format, TEX_FILTER_FLAGS.DEFAULT, ThresholdDefault);
                workingScratch.Dispose();
                return result;
            }
            else
            {
                return workingScratch;
            }
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

        public static ScratchImage EquirectangularToCubemapGPU(ID3D11Device device, Image[] envMaps, int cubemapSize, bool usePrefilterSize, bool mirrorCubemap)
        {
            Debug.Assert(device != null);
            Debug.Assert(envMaps != null && envMaps.Length > 0);

            if (usePrefilterSize)
            {
                cubemapSize = PrefilteredSpecularCubemapSize;
            }

            var format = envMaps[0].Format;
            int arraySize = envMaps.Length * 6;
            int cubemapCount = arraySize / 6;
            int mipLevels = 1;

            using EquirectangularToCubeMap shader = new(device, cubemapSize, arraySize, format);
            if (!shader.Run(envMaps, mirrorCubemap)) return null;

            var result = Helper.InitializeCube(format, cubemapSize, cubemapSize, cubemapCount, mipLevels, CP_FLAGS.NONE);
            if (!shader.Download(result, mipLevels)) return null;

            return result;
        }

        public static ScratchImage PrefilterDiffuse(ID3D11Device device, ScratchImage cubemaps, int sampleCount)
        {
            Debug.Assert(device != null);

            var metaData = cubemaps.GetMetadata();
            int arraySize = metaData.ArraySize;
            int cubeMapSize = PrefilteredDiffuseCubemapSize;
            int cubemapCount = arraySize / 6;
            var format = metaData.Format;
            int mipLevels = 1;
            Debug.Assert(metaData.IsCubemap() && cubemapCount > 0 && (arraySize % 6) == 0);

            using PrefilterDiffuseEnvMap shader = new(device, cubemaps, arraySize, cubeMapSize, cubemapCount, format, sampleCount);
            if (!shader.Run()) return null;

            var result = Helper.InitializeCube(format, cubeMapSize, cubeMapSize, cubemapCount, mipLevels, CP_FLAGS.NONE);
            if (!shader.Download(result, mipLevels)) return null;

            return result;
        }
        public static ScratchImage PrefilterSpecular(ID3D11Device device, ScratchImage cubemaps, int sampleCount)
        {
            Debug.Assert(device != null);

            var metaData = cubemaps.GetMetadata();
            int arraySize = metaData.ArraySize;
            int cubeMapSize = PrefilteredSpecularCubemapSize;
            int cubemapCount = arraySize / 6;
            var format = metaData.Format;
            int mipLevels = RoughnessMipLevels;
            Debug.Assert(metaData.IsCubemap() && cubemapCount > 0 && (arraySize % 6) == 0);

            using PrefilterSpecularEnvMap shader = new(device, cubemaps, arraySize, cubeMapSize, cubemapCount, format, sampleCount);
            if (!shader.Run()) return null;

            var result = Helper.InitializeCube(format, cubeMapSize, cubeMapSize, cubemapCount, mipLevels, CP_FLAGS.NONE);
            if (!shader.Download(result, mipLevels)) return null;

            Debug.Assert(metaData.Width == result.GetMetadata().Width);
            // Copy mip 0 from the source cubemap
            for (int imgIdx = 0; imgIdx < arraySize; imgIdx++)
            {
                var src = cubemaps.GetImage(0, imgIdx, 0);
                var dst = result.GetImage(0, imgIdx, 0);

                BlobStreamWriter writer = new(dst.Pixels, (int)dst.SlicePitch);
                writer.Write(src.Pixels, (int)src.SlicePitch);
            }

            return result;
        }

        public static ScratchImage BrdfIntegrationLut(ID3D11Device device, int sampleCount)
        {
            Debug.Assert(device != null);

            int lutSize = BrdfIntegrationLutSize;
            var format = DXGI_FORMAT.R16G16_FLOAT;

            using BrdfIntegrationLut shader = new(device, lutSize, format, sampleCount);
            if (!shader.Run()) return null;

            var result = Helper.Initialize2D(format, lutSize, lutSize, 1, 1, CP_FLAGS.NONE);
            if (!shader.Download(result, 1)) return null;
            return result;
        }
    }
}
