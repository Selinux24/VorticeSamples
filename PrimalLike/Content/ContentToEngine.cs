using PrimalLike.Common;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Utilities;

namespace PrimalLike.Content
{
    /// <summary>
    /// Content to engine resource helper.
    /// </summary>
    public static class ContentToEngine
    {
        /// <summary>
        /// This constant indicates that an element in GeometryHierarchies is not a pointer, but a gpuId
        /// </summary>
        private const IntPtr SingleMeshMarker = 0x01;
        private static readonly int ShiftBits = (Marshal.SizeOf<IntPtr>() - sizeof(IdType)) << 3;
        private static readonly FreeList<IntPtr> GeometryHierarchies = new();
        private static readonly object GeometryMutex = new();

        private static readonly List<IntPtr> Shaders = [];
        private static readonly object ShaderMutex = new();

        public static uint CreateResource(MemoryStream stream, AssetTypes assetType)
        {
            IntPtr data = IntPtr.Zero;
            try
            {
                byte[] buffer = stream.ToArray();
                data = Marshal.AllocHGlobal(buffer.Length);
                Marshal.Copy(buffer, 0, data, buffer.Length);
                return CreateResource(data, assetType);
            }
            finally
            {
                if (data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(data);
                }
            }
        }
        public static uint CreateResource(IntPtr data, AssetTypes assetType)
        {
            Debug.Assert(data != IntPtr.Zero);
            uint id = assetType switch
            {
                AssetTypes.Mesh => CreateGeometryResource(data),
                _ => uint.MaxValue,
            };

            Debug.Assert(id != uint.MaxValue);
            return id;
        }
        public static void DestroyResource(uint id, AssetTypes assetType)
        {
            switch (assetType)
            {
                case AssetTypes.Mesh:
                    DestroyGeometryResource(id);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static uint CreateGeometryResource(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            return IsSingleMesh(data) ? CreateSingleSubmesh(data) : CreateMeshHierarchy(data);
        }
        private static bool IsSingleMesh(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var blob = new BlobStreamReader(data);
            var lodCount = blob.Read<uint>();
            Debug.Assert(lodCount > 0);
            if (lodCount > 1) return false;

            blob.Skip(sizeof(float));
            var submeshCount = blob.Read<uint>();
            Debug.Assert(submeshCount > 0);
            return submeshCount == 1;
        }
        private static uint CreateSingleSubmesh(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            BlobStreamReader blob = new(data);

            // skip lod_count, lod_threshold, submesh_count and size_of_submeshes
            blob.Skip(sizeof(uint) + sizeof(float) + sizeof(uint) + sizeof(uint));

            IntPtr at = blob.Position;
            uint gpuId = Renderer.AddSubmesh(ref at);

            // create a fake pointer and put it in the geometry_hierarchies.
            IntPtr fakePointer = CreateGpuIdFakePointer(gpuId);
            lock (GeometryMutex)
            {
                return (uint)GeometryHierarchies.Add(fakePointer);
            }
        }
        private static IntPtr CreateGpuIdFakePointer(uint gpuId)
        {
            Debug.Assert(Marshal.SizeOf<IntPtr>() > sizeof(IdType));
            return new(((IntPtr)gpuId << ShiftBits) | SingleMeshMarker);
        }
        private static uint CreateMeshHierarchy(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            int size = GetGeometryHierarchyBufferSize(data);
            BlobStreamReader blob = new(data);

            uint lodCount = blob.Read<uint>();
            Debug.Assert(lodCount > 0);

            GeometryHierarchyStream stream = new(size, lodCount);
            uint submeshIndex = 0;

            for (int lodIdx = 0; lodIdx < lodCount; ++lodIdx)
            {
                float trheshold = blob.Read<float>();
                stream.Thresholds.Add(trheshold);

                uint id = blob.Read<uint>();
                Debug.Assert(id < ushort.MaxValue);
                ushort idCount = (ushort)id;
                stream.LodOffsets.Add(new()
                {
                    Offset = (ushort)submeshIndex,
                    Count = idCount
                });

                // skip over size_of_submeshes
                blob.Skip(sizeof(uint));

                for (ushort idIdx = 0; idIdx < idCount; idIdx++)
                {
                    IntPtr at = blob.Position;
                    stream.GpuIds.Add(Renderer.AddSubmesh(ref at));
                    submeshIndex++;
                    blob.Skip((uint)(at - blob.Position));
                    Debug.Assert(submeshIndex < ushort.MaxValue);
                }
            }

            Debug.Assert(ValidateThresholdValues(stream, lodCount));

            Debug.Assert(Marshal.SizeOf(typeof(IntPtr)) > 2, "We need the least significant bit for the single mesh marker.");
            lock (GeometryMutex)
            {
                return (uint)GeometryHierarchies.Add(stream.GetHierarchyBuffer());
            }
        }
        private static int GetGeometryHierarchyBufferSize(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            BlobStreamReader blob = new(data);

            uint lodCount = blob.Read<uint>();
            Debug.Assert(lodCount > 0);
            int size = sizeof(uint) + (sizeof(float) + LodOffset.Stride) * (int)lodCount;

            for (var lodIdx = 0; lodIdx < lodCount; lodIdx++)
            {
                // skip threshold
                blob.Skip(sizeof(float));

                // add size of gpu_ids (sizeof(IdType) * submesh_count)
                uint submeshCount = blob.Read<uint>();
                size += sizeof(IdType) * (int)submeshCount;

                // skip submesh data and go to the next LOD
                uint sizeOfSubmeshes = blob.Read<uint>();
                blob.Skip(sizeOfSubmeshes);
            }

            return size;
        }
        private static bool ValidateThresholdValues(GeometryHierarchyStream stream, uint lodCount)
        {
            var previousThreshold = stream.Thresholds[0];

            for (var i = 1; i < lodCount; ++i)
            {
                if (stream.Thresholds[i] <= previousThreshold)
                {
                    return false;
                }

                previousThreshold = stream.Thresholds[i];
            }

            return true;
        }

        private static void DestroyGeometryResource(uint id)
        {
            lock (GeometryMutex)
            {
                IntPtr pointer = GeometryHierarchies[(int)id];
                if ((pointer & SingleMeshMarker) != 0)
                {
                    IdType fakePointer = GpuIdFromFakePointer(pointer);
                    Renderer.RemoveSubmesh(fakePointer);
                }
                else
                {
                    GeometryHierarchyStream stream = new(pointer);
                    uint lodCount = stream.LodCount;
                    int idIndex = 0;
                    for (int lod = 0; lod < lodCount; lod++)
                    {
                        for (ushort i = 0; i < stream.LodOffsets[lod].Count; i++)
                        {
                            uint submeshId = stream.GpuIds[idIndex++];
                            Renderer.RemoveSubmesh(submeshId);
                        }
                    }

                    Marshal.FreeHGlobal(pointer);
                }

                GeometryHierarchies.Remove((int)id);
            }
        }
        private static IdType GpuIdFromFakePointer(IntPtr pointer)
        {
            Debug.Assert((pointer & SingleMeshMarker) != 0);
            return (IdType)((pointer >> ShiftBits) & IdDetail.InvalidId);
        }

        public static IdType AddShader(IntPtr data)
        {
            CompiledShader shaderPtr = Marshal.PtrToStructure<CompiledShader>(data);

            ulong size = sizeof(ulong) + CompiledShader.HashLength + shaderPtr.ByteCodeSize;
            var shader = shaderPtr.ByteCode;
            IntPtr shaderData = Marshal.AllocHGlobal((int)size);
            Marshal.Copy(shader, 0, shaderData, (int)size);

            lock (ShaderMutex)
            {
                Shaders.Add(shaderData);
                return (IdType)Shaders.Count - 1;
            }
        }
        public static void RemoveShader(IdType id)
        {
            lock (ShaderMutex)
            {
                Debug.Assert(IdDetail.IsValid(id));
                Shaders[(int)id] = IntPtr.Zero;
            }
        }
        public static IntPtr GetShader(IdType id)
        {
            lock (ShaderMutex)
            {
                Debug.Assert(IdDetail.IsValid(id));

                return Shaders[(int)id];
            }
        }
    }
}
