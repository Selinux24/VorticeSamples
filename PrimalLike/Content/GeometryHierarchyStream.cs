using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    class GeometryHierarchyStream
    {
        private readonly IntPtr buffer;
        private readonly uint lodCount;
        private readonly float[] thresholds;
        private readonly LodOffset[] lodOffsets;
        private readonly uint[] gpuIds;

        public uint LodCount { get => lodCount; }
        public float[] Thresholds { get => thresholds; }
        public LodOffset[] LodOffsets { get => lodOffsets; }
        public uint[] GpuIds { get => gpuIds; }

        public GeometryHierarchyStream(IntPtr buffer, uint lodCount = uint.MaxValue)
        {
            Debug.Assert(buffer != IntPtr.Zero && lodCount > 0);

            this.buffer = buffer;

            if (lodCount != uint.MaxValue)
            {
                Marshal.WriteInt32(buffer, (int)lodCount);
            }

            this.lodCount = lodCount;
            thresholds = new float[this.lodCount];
            lodOffsets = new LodOffset[this.lodCount];
            gpuIds = new IdType[this.lodCount];
        }

        public void GetGpuIds(uint lod, out int[] ids, out uint idCount)
        {
            Debug.Assert(lod < lodCount);
            ids = new int[lodOffsets[lod].Count];
            idCount = lodOffsets[lod].Count;
            Marshal.Copy(buffer + lodOffsets[lod].Offset, ids, 0, (int)idCount);
        }
        public uint LodFromThreshold(float threshold)
        {
            Debug.Assert(threshold > 0);

            for (uint i = lodCount - 1; i > 0; i--)
            {
                if (thresholds[i] <= threshold)
                {
                    return i;
                }
            }

            Debug.Assert(false); // shouldn't ever get here.
            return 0;
        }
    }
}
