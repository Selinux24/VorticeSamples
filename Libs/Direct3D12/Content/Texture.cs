using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        ///<summary>
        ///NOTE: expects data to contain
        ///struct {
        ///    u32 width, height, array_size (or depth), flags, mip_levels, format,
        ///    struct {
        ///        u32 row_pitch, slice_pitch,
        ///        u8 image[mip_level][slice_pitch * depth_per_mip],
        ///    } images[]
        ///} texture
        ///</summary>
        private static D3D12Texture CreateResourceFromTextureData(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            BlobStreamReader blob = new(data);
            uint width = blob.Read<uint>();
            uint height = blob.Read<uint>();
            uint depth = 1;
            uint arraySize = blob.Read<uint>();
            TextureFlags flags = (TextureFlags)blob.Read<uint>();
            uint mipLevels = blob.Read<uint>();
            Format format = (Format)blob.Read<uint>();
            bool is3d = flags.HasFlag(TextureFlags.IsVolumeMap);

            Debug.Assert(mipLevels <= D3D12Texture.MaxMips);
            uint[] depthPerMipLevel = [.. Enumerable.Repeat(1u, D3D12Texture.MaxMips)];

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
                    uint rowPitch = blob.Read<uint>();
                    uint slicePitch = blob.Read<uint>();

                    subresources.Add(new SubresourceData(blob.Position, (nint)rowPitch, (nint)slicePitch));

                    // skip the rest of slices.
                    blob.Skip(slicePitch * depthPerMipLevel[j]);
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

            PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[subresourceCount];
            uint[] numRows = new uint[subresourceCount];
            ulong[] rowSizes = new ulong[subresourceCount];

            var device = D3D12Graphics.Device;
            device.GetCopyableFootprints(desc, 0, subresourceCount, 0, layouts, numRows, rowSizes, out ulong requiredSize);

            Debug.Assert(requiredSize > 0);
            UploadContext context = new((uint)requiredSize);
            unsafe
            {
                IntPtr cpuAddress = (IntPtr)context.CpuAddress;

                for (int subresourceIdx = 0; subresourceIdx < subresourceCount; subresourceIdx++)
                {
                    PlacedSubresourceFootPrint layout = layouts[subresourceIdx];
                    uint subresourceHeight = numRows[subresourceIdx];
                    uint subresourceDepth = layout.Footprint.Depth;
                    SubresourceData subResource = subresources[subresourceIdx];

                    IntPtr destDataPtr = cpuAddress + (nint)layout.Offset;
                    uint destRowPitch = layout.Footprint.RowPitch;
                    uint destSlicePitch = layout.Footprint.RowPitch * subresourceHeight;

                    for (uint depthIdx = 0; depthIdx < subresourceDepth; depthIdx++)
                    {
                        IntPtr srcSlice = (IntPtr)subResource.pData + (nint)(subResource.SlicePitch * depthIdx);
                        IntPtr dstSlice = destDataPtr + (nint)(destSlicePitch * depthIdx);

                        for (uint rowIdx = 0; rowIdx < subresourceHeight; rowIdx++)
                        {
                            IntPtr source = srcSlice + (nint)((uint)subResource.RowPitch * rowIdx);
                            IntPtr destination = dstSlice + (nint)(destRowPitch * rowIdx);
                            nuint byteCount = (nuint)rowSizes[subresourceIdx];
                            NativeMemory.Copy(source.ToPointer(), destination.ToPointer(), byteCount);
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

                context.CmdList.CopyTextureRegion(dst, 0, 0, 0, src);
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

        public static void GetDescriptorIndices(uint[] textureIds, ref uint[] indices)
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
