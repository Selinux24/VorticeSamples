using PrimalLike.Common;
using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    class GeometryHierarchyStream
    {
        public struct LodOffset
        {
            public ushort Offset;
            public ushort Count;
        }

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

            for (uint i = lodCount - 1; i > 0; --i)
            {
                if (thresholds[i] <= threshold) return i;
            }

            Debug.Assert(false); // shouldn't ever get here.
            return 0;
        }


        private static readonly IntPtr SingleMeshMarker = new(0x01);
        private static readonly List<IntPtr> GeometryHierarchies = [];
        private static readonly object GeometryMutex = new();

        public static uint GetGeometryHierarchyBufferSize(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var blob = new BlobStreamReader(data);
            var lodCount = blob.ReadUInt32();
            Debug.Assert(lodCount > 0);
            var size = sizeof(uint) + (sizeof(float) + Marshal.SizeOf<GeometryHierarchyStream.LodOffset>()) * (int)lodCount;

            for (var lodIdx = 0; lodIdx < lodCount; ++lodIdx)
            {
                blob.Skip(sizeof(float));
                size += sizeof(IdType) * (int)blob.ReadUInt32();
                blob.Skip((int)blob.ReadUInt32());
            }

            return (uint)size;
        }

        public static int CreateMeshHierarchy(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var size = GetGeometryHierarchyBufferSize(data);
            var hierarchyBuffer = Marshal.AllocHGlobal((int)size);

            var blob = new BlobStreamReader(data);
            var lodCount = blob.ReadUInt32();
            Debug.Assert(lodCount > 0);
            var stream = new GeometryHierarchyStream(hierarchyBuffer, lodCount);
            ushort submeshIndex = 0;
            var gpuIds = stream.GpuIds;

            for (var lodIdx = 0; lodIdx < lodCount; ++lodIdx)
            {
                stream.Thresholds[lodIdx] = blob.ReadFloat();
                var idCount = blob.ReadUInt32();
                Debug.Assert(idCount < (1 << 16));
                stream.LodOffsets[lodIdx] = new LodOffset { Offset = submeshIndex, Count = (ushort)idCount };
                blob.Skip(sizeof(uint));
                for (var idIdx = 0; idIdx < idCount; ++idIdx)
                {
                    var at = blob.Position;
                    gpuIds[submeshIndex++] = (uint)Renderer.AddSubmesh(at);
                    blob.Skip(at - blob.Position);
                    Debug.Assert((uint)submeshIndex < (1 << 16));
                }
            }

            Debug.Assert(IsValid(stream, lodCount));

            lock (GeometryMutex)
            {
                GeometryHierarchies.Add(hierarchyBuffer);
                return GeometryHierarchies.Count - 1;
            }
        }
        private static bool IsValid(GeometryHierarchyStream stream, uint lodCount)
        {
            var previousThreshold = stream.Thresholds[0];
            for (var i = 1; i < lodCount; ++i)
            {
                if (stream.Thresholds[i] <= previousThreshold) return false;
                previousThreshold = stream.Thresholds[i];
            }
            return true;
        }
        public static int CreateSingleSubmesh(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var blob = new BlobStreamReader(data);
            blob.Skip(sizeof(uint) + sizeof(float) + sizeof(uint) + sizeof(uint));
            var at = blob.Position;
            var gpuId = Renderer.AddSubmesh(at);

            var fakePointer = new IntPtr(((long)gpuId << 32) | SingleMeshMarker.ToInt64());
            lock (GeometryMutex)
            {
                GeometryHierarchies.Add(fakePointer);
                return GeometryHierarchies.Count - 1;
            }
        }
        public static bool IsSingleMesh(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var blob = new BlobStreamReader(data);
            var lodCount = blob.ReadUInt32();
            Debug.Assert(lodCount > 0);
            if (lodCount > 1) return false;

            blob.Skip(sizeof(float));
            var submeshCount = blob.ReadUInt32();
            Debug.Assert(submeshCount > 0);
            return submeshCount == 1;
        }
        public static IdType GpuIdFromFakePointer(IntPtr pointer)
        {
            Debug.Assert((pointer.ToInt64() & SingleMeshMarker.ToInt64()) != 0);
            return (IdType)((pointer.ToInt64() >> 32) & IdType.MaxValue);
        }
        public static int CreateGeometryResource(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            return IsSingleMesh(data) ? CreateSingleSubmesh(data) : CreateMeshHierarchy(data);
        }
        public static void DestroyGeometryResource(int id)
        {
            lock (GeometryMutex)
            {
                var pointer = GeometryHierarchies[id];
                if ((pointer.ToInt64() & SingleMeshMarker.ToInt64()) != 0)
                {
                    Renderer.RemoveSubmesh((int)GpuIdFromFakePointer(pointer));
                }
                else
                {
                    var stream = new GeometryHierarchyStream(pointer);
                    var lodCount = stream.LodCount;
                    var idIndex = 0;
                    for (var lod = 0; lod < lodCount; ++lod)
                    {
                        for (var i = 0; i < stream.LodOffsets[lod].Count; ++i)
                        {
                            Renderer.RemoveSubmesh((int)stream.GpuIds[idIndex++]);
                        }
                    }

                    Marshal.FreeHGlobal(pointer);
                }

                GeometryHierarchies.Remove(id);
            }
        }
    }
}
