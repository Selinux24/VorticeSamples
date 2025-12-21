using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Utilities;

namespace TexturesImporter
{
    public struct TextureData()
    {
        public const uint MaxMips = 14; // we support up to 8k textures.
        public IntPtr SubresourceData = 0;
        public uint SubresourceSize = 0;
        public IntPtr Icon = 0;
        public uint IconSize = 0;
        public TextureInfo Info = new();
        public TextureImportSettings ImportSettings = new();

        /// <summary>
        /// Saves the texture to a file
        /// </summary>
        /// <param name="outputPath">Output path</param>
        public readonly void SaveTexture(string outputPath)
        {
            if (SubresourceData == IntPtr.Zero || SubresourceSize == 0)
            {
                return;
            }

            FileUtils.MakeRoom(outputPath);

            byte[] data = PackForEngine();
            File.WriteAllBytes(outputPath, data);
        }
        private readonly byte[] PackForEngine()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((uint)Info.Width);
            writer.Write((uint)Info.Height);
            writer.Write((uint)Info.ArraySize);
            writer.Write((uint)Info.Flags);
            writer.Write((uint)Info.MipLevels);
            writer.Write(Info.Format);

            var slices = GetSlices();

            Debug.Assert(slices.Count > 0);
            foreach (var arraySlice in slices)
            {
                foreach (var mipLevel in arraySlice)
                {
                    writer.Write(mipLevel[0].RowPitch);
                    writer.Write(mipLevel[0].SlicePitch);
                    foreach (var slice in mipLevel)
                    {
                        writer.Write(slice.RawContent);
                    }
                }
            }

            writer.Flush();
            var data = (writer.BaseStream as MemoryStream)?.ToArray();
            Debug.Assert(data?.Length > 0);

            return data;
        }
        private readonly List<List<List<Slice>>> GetSlices()
        {
            Debug.Assert(Info.MipLevels > 0);
            Debug.Assert(SubresourceData != IntPtr.Zero && SubresourceSize > 0);

            var subresourceData = new byte[SubresourceSize];
            Marshal.Copy(SubresourceData, subresourceData, 0, (int)SubresourceSize);

            return SlicesFromBinary(
                subresourceData,
                Info.ArraySize,
                Info.MipLevels,
                Info.Flags.HasFlag(TextureFlags.IsVolumeMap));
        }
        private static List<List<List<Slice>>> SlicesFromBinary(byte[] data, int arraySize, int mipLevels, bool is3D)
        {
            Debug.Assert(data?.Length > 0 && arraySize > 0);
            Debug.Assert(mipLevels > 0 && mipLevels < MaxMips);

            int[] depthPerMipLevel = [.. Enumerable.Repeat(1, mipLevels)];

            if (is3D)
            {
                var depth = arraySize;
                arraySize = 1;
                for (var i = 0; i < mipLevels; i++)
                {
                    depthPerMipLevel[i] = depth;
                    depth = Math.Max(depth >> 1, 1);
                }
            }

            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            List<List<List<Slice>>> slices = [];
            for (var i = 0; i < arraySize; i++)
            {
                List<List<Slice>> arraySlice = [];
                for (var j = 0; j < mipLevels; j++)
                {
                    List<Slice> mipSlice = [];
                    for (var k = 0; k < depthPerMipLevel[j]; k++)
                    {
                        Slice slice = new()
                        {
                            Width = reader.ReadUInt32(),
                            Height = reader.ReadUInt32(),
                            RowPitch = reader.ReadUInt32(),
                            SlicePitch = reader.ReadUInt32()
                        };
                        slice.RawContent = reader.ReadBytes((int)slice.SlicePitch);

                        mipSlice.Add(slice);
                    }

                    arraySlice.Add(mipSlice);
                }

                slices.Add(arraySlice);
            }

            return slices;
        }

        public readonly TextureData Copy()
        {
            TextureData data = new();

            if (SubresourceData != IntPtr.Zero && SubresourceSize > 0)
            {
                var bytes = new byte[SubresourceSize];
                data.SubresourceData = Marshal.AllocCoTaskMem((int)SubresourceSize);
                data.SubresourceSize = SubresourceSize;
                Marshal.Copy(SubresourceData, bytes, 0, (int)SubresourceSize);
                Marshal.Copy(bytes, 0, data.SubresourceData, (int)SubresourceSize);
            }

            if (Icon != IntPtr.Zero && IconSize > 0)
            {
                var bytes = new byte[IconSize];
                data.Icon = Marshal.AllocCoTaskMem((int)IconSize);
                data.IconSize = IconSize;
                Marshal.Copy(Icon, bytes, 0, (int)IconSize);
                Marshal.Copy(bytes, 0, data.Icon, (int)IconSize);
            }

            data.Info = Info;
            data.ImportSettings = ImportSettings;

            return data;
        }
    }
}
