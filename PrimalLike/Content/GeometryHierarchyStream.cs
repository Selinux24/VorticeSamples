using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utilities;

namespace PrimalLike.Content
{
    class GeometryHierarchyStream
    {
        private readonly IntPtr buffer;
        private readonly int bufferSize;
        private readonly uint lodCount;

        public uint LodCount { get => lodCount; }
        public List<float> Thresholds { get; } = [];
        public List<LodOffset> LodOffsets { get; } = [];
        public List<uint> GpuIds { get; } = [];

        public GeometryHierarchyStream(int size, uint lodCount = uint.MaxValue)
        {
            Debug.Assert(size > 0 && lodCount > 0);

            buffer = Marshal.AllocHGlobal(size);
            bufferSize = size;

            if (lodCount != uint.MaxValue)
            {
                Marshal.WriteInt32(buffer, (int)lodCount);
            }

            this.lodCount = lodCount;
        }
        public GeometryHierarchyStream(IntPtr buffer)
        {
            Debug.Assert(buffer != IntPtr.Zero);

            this.buffer = buffer;

            BlobStreamReader reader = new(buffer);

            lodCount = reader.Read<uint>();
            for (uint i = 0; i < lodCount; i++)
            {
                Thresholds.Add(reader.Read<float>());
                LodOffsets.Add(new()
                {
                    Offset = reader.Read<ushort>(),
                    Count = reader.Read<ushort>()
                });

                for (int j = 0; j < LodOffsets[(int)i].Count; j++)
                {
                    GpuIds.Add(reader.Read<uint>());
                }
            }
        }

        public IntPtr GetHierarchyBuffer()
        {
            // Write all the collected data in the buffer
            BlobStreamWriter writer = new(buffer, bufferSize);

            writer.Write(lodCount);
            for (int i = 0; i < lodCount; i++)
            {
                writer.Write(Thresholds[i]);

                writer.Write(LodOffsets[i].Offset);
                writer.Write(LodOffsets[i].Count);

                for (int j = 0; j < LodOffsets[i].Count; j++)
                {
                    writer.Write(GpuIds[j]);
                }
            }

            return buffer;
        }
        public void GetGpuIds(uint lod, out uint[] ids, out uint idCount)
        {
            Debug.Assert(lod < lodCount);
            var offset = LodOffsets[(int)lod];
            idCount = offset.Count;
            ids = new uint[idCount];
            Array.Copy(GpuIds.ToArray(), offset.Offset, ids, 0, (int)idCount);
        }
        public uint LodFromThreshold(float threshold)
        {
            Debug.Assert(threshold > 0);

            for (uint i = lodCount - 1; i > 0; i--)
            {
                if (Thresholds[(int)i] <= threshold)
                {
                    return i;
                }
            }

            Debug.Assert(false); // shouldn't ever get here.
            return 0;
        }
    }
}
