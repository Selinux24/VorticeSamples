using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.Direct3D12;
using Vortice.DXGI;
using static Direct3D12.D3D12Upload;

namespace Direct3D12.Content
{
    static class Texture
    {
        static readonly FreeList<D3D12Texture> textures = new();
        static readonly FreeList<uint> descriptorIndices = new();
        static readonly object textureMutex = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {

        }

        public static uint Add(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            D3D12Texture texture = CreateResourceFromTextureData(data);

            lock (textureMutex)
            {
                uint id = textures.Add(texture);
                descriptorIndices.Add(textures[id].Srv.Index);
                return id;
            }
        }
        private static D3D12Texture CreateResourceFromTextureData(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            BlobStreamReader blob = new(data);
            uint width = blob.Read<uint>();
            uint height = blob.Read<uint>();
            uint depth = 1;
            uint arraySize = blob.Read<uint>();
            TextureFlags flags = blob.Read<TextureFlags>();
            uint mipLevels = blob.Read<uint>();
            Format format = blob.Read<Format>();
            bool is3d = flags.HasFlag(TextureFlags.IsVolumeMap);

            Debug.Assert(mipLevels <= D3D12Texture.MaxMips);

            uint[] depthPerMipLevel = new uint[D3D12Texture.MaxMips];
            for (uint i = 0; i < D3D12Texture.MaxMips; i++)
            {
                depthPerMipLevel[i] = 1;
            }

            if (is3d)
            {
                depth = arraySize;
                arraySize = 1;
                uint depthPerMip = depth;

                for (uint i = 0; i < mipLevels; i++)
                {
                    depthPerMipLevel[i] = depthPerMip;
                    depthPerMip = Math.Max(depthPerMip >> 1, 1);
                }
            }

            List<SubresourceData> subresources = [];

            for (uint i = 0; i < arraySize; i++)
            {
                for (uint j = 0; j < mipLevels; j++)
                {
                    blob.Skip(2 * sizeof(uint)); // skip width and height
                    int rowPitch = blob.Read<int>();
                    int slicePitch = blob.Read<int>();

                    subresources.Add(new SubresourceData(blob.Position, rowPitch, slicePitch));

                    blob.Skip(slicePitch);

                    // skip the rest of the slices fo 3d textures with depth > 1
                    for (uint k = 1; k < depthPerMipLevel[j]; k++)
                    {
                        blob.Skip(4 * sizeof(uint) + slicePitch);
                    }
                }
            }

            ResourceDescription desc = new()
            {
                Dimension = is3d ? ResourceDimension.Texture3D : ResourceDimension.Texture2D, // TODO: handle 1D textures.
                Alignment = 0,
                Width = width,
                Height = height,
                DepthOrArraySize = is3d ? (ushort)depth : (ushort)arraySize,
                MipLevels = (ushort)mipLevels,
                Format = format,
                SampleDescription = new(1, 0),
                Layout = TextureLayout.Unknown,
                Flags = ResourceFlags.None
            };

            Debug.Assert(!(flags.HasFlag(TextureFlags.IsCubeMap) && (arraySize % 6) != 0));
            uint subresourceCount = arraySize * mipLevels;
            Debug.Assert(subresourceCount > 0);

            PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[Marshal.SizeOf<PlacedSubresourceFootPrint>() * subresourceCount];
            uint[] numRows = new uint[sizeof(uint) * subresourceCount];
            ulong[] rowSizes = new ulong[sizeof(ulong) * subresourceCount];

            var device = D3D12Graphics.Device;
            device.GetCopyableFootprints(desc, 0, subresourceCount, 0, layouts, numRows, rowSizes, out ulong requiredSize);

            Debug.Assert(requiredSize > 0);
            UploadContext context = new((uint)requiredSize);
            unsafe
            {
                void* cpuAddress = context.CpuAddress;

                for (int subresourceIdx = 0; subresourceIdx < subresourceCount; subresourceIdx++)
                {
                    var layout = layouts[subresourceIdx];
                    uint subresourceHeight = numRows[subresourceIdx];
                    uint subresourceDepth = layout.Footprint.Depth;
                    var subResource = subresources[subresourceIdx];

                    void* destData = Unsafe.Add<byte>(cpuAddress, (int)layout.Offset);
                    var destRowPitch = layout.Footprint.RowPitch;
                    var destSlicePitch = layout.Footprint.RowPitch * subresourceHeight;

                    for (uint depthIdx = 0; depthIdx < subresourceDepth; depthIdx++)
                    {
                        void* srcSlice = Unsafe.Add<byte>(subResource.pData, (int)(subResource.SlicePitch * depthIdx));
                        void* dstSlice = Unsafe.Add<byte>(destData, (int)(destSlicePitch * depthIdx));

                        for (uint rowIdx = 0; rowIdx < subresourceHeight; rowIdx++)
                        {
                            void* source = Unsafe.Add<byte>(dstSlice, (int)(destRowPitch * rowIdx));
                            void* destination = Unsafe.Add<byte>(srcSlice, (int)(subResource.RowPitch * rowIdx));
                            NativeMemory.Copy(source, destination, (nuint)rowSizes[subresourceIdx]);
                        }
                    }
                }
            }

            D3D12Helpers.DxCall(device.CreateCommittedResource(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                desc,
                ResourceStates.Common,
                null,
                out var resource));

            var uploadBuffer = context.UploadBuffer;
            for (uint i = 0; i < subresourceCount; i++)
            {
                TextureCopyLocation src = new(uploadBuffer, layouts[i]);

                TextureCopyLocation dst = new(resource, i);

                context.CmdList.CopyTextureRegion(dst, 0, 0, 0, src, null);
            }

            context.EndUpload();

            Debug.Assert(resource != null);

            ShaderResourceViewDescription srvDesc = new();
            D3D12TextureInitInfo info = new()
            {
                Resource = resource
            };

            if (flags.HasFlag(TextureFlags.IsCubeMap))
            {
                Debug.Assert(arraySize % 6 == 0);
                srvDesc.Format = format;
                srvDesc.Shader4ComponentMapping = ShaderComponentMapping.Default;

                if (arraySize > 6)
                {
                    srvDesc.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                    srvDesc.TextureCubeArray.MostDetailedMip = 0;
                    srvDesc.TextureCubeArray.MipLevels = mipLevels;
                    srvDesc.TextureCubeArray.NumCubes = arraySize / 6;
                }
                else
                {
                    srvDesc.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                    srvDesc.TextureCube.MostDetailedMip = 0;
                    srvDesc.TextureCube.MipLevels = mipLevels;
                    srvDesc.TextureCube.ResourceMinLODClamp = 0.0f;
                }

                info.SrvDesc = srvDesc;
            }

            return new D3D12Texture(info);
        }

        public static void Remove(uint id)
        {
            lock (textureMutex)
            {
                textures.Remove(id);
                descriptorIndices.Remove(id);
            }
        }

        public static void GetDescriptorIndices(uint[] textureIds, uint[] indices)
        {
            Debug.Assert(textureIds != null && indices != null);
            lock (textureMutex)
            {
                for (uint i = 0; i < textureIds.Length; i++)
                {
                    indices[i] = descriptorIndices[textureIds[i]];
                }
            }
        }
    }
}
