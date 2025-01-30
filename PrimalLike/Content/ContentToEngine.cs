using PrimalLike.Common;
using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
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
        private static readonly int shiftBits = (Marshal.SizeOf<IntPtr>() - sizeof(IdType)) << 3;
        private static readonly FreeList<IntPtr> geometryHierarchies = new();
        private static readonly object geometryMutex = new();

        private static readonly FreeList<Dictionary<uint, CompiledShader>> shaderGroups = new();
        private static readonly object shaderMutex = new();

        public static uint CreateResource<T>(T data, AssetTypes assetType)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data));
            Marshal.StructureToPtr(data, ptr, false);

            return CreateResource(ptr, assetType);
        }
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
                AssetTypes.Material => CreateMaterialResource(data),
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
                case AssetTypes.Material:
                    DestroyMaterialResource(id);
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
            lock (geometryMutex)
            {
                return geometryHierarchies.Add(fakePointer);
            }
        }
        private static IntPtr CreateGpuIdFakePointer(uint gpuId)
        {
            Debug.Assert(Marshal.SizeOf<IntPtr>() > sizeof(IdType));
            return new(((IntPtr)gpuId << shiftBits) | SingleMeshMarker);
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
            lock (geometryMutex)
            {
                return geometryHierarchies.Add(stream.GetHierarchyBuffer());
            }
        }
        private static int GetGeometryHierarchyBufferSize(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            BlobStreamReader blob = new(data);

            uint lodCount = blob.Read<uint>();
            Debug.Assert(lodCount > 0);
            // add size of  lod_count, thresholds and lod offsets to the size of hierarchy.
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
        public static void GetSubmeshGpuIds(IdType geometryContentId, uint idCount, out IdType[] gpuIds)
        {
            lock (geometryMutex)
            {
                var pointer = geometryHierarchies[geometryContentId];
                if ((pointer & SingleMeshMarker) != 0)
                {
                    Debug.Assert(idCount == 1);

                    gpuIds = [GpuIdFromFakePointer(pointer)];
                }
                else
                {
                    GeometryHierarchyStream stream = new(pointer);
                    Debug.Assert(ValidateLods(stream, idCount));

                    gpuIds = [.. stream.GpuIds];
                }
            }
        }
        private static bool ValidateLods(GeometryHierarchyStream stream, uint idCount)
        {
            uint lodCount = stream.LodCount;
            LodOffset lodOffset = stream.LodOffsets[(int)lodCount - 1];
            uint gpuIdCount = lodOffset.Offset + (uint)lodOffset.Count;
            return gpuIdCount == idCount;
        }
        public static void GetLodOffsets(IdType[] geometryIds, float[] thresholds, uint idCount, List<LodOffset> offsets)
        {
            Debug.Assert(geometryIds != null && thresholds != null && idCount != 0);
            Debug.Assert(offsets.Count == 0);

            lock (geometryMutex)
            {
                for (uint i = 0; i < idCount; i++)
                {
                    var pointer = geometryHierarchies[geometryIds[i]];
                    if ((pointer & SingleMeshMarker) != 0)
                    {
                        Debug.Assert(idCount == 1);
                        offsets.Add(new(0, 1));
                    }
                    else
                    {
                        GeometryHierarchyStream stream = new(pointer);
                        uint lod = stream.LodFromThreshold(thresholds[i]);
                        offsets.Add(stream.LodOffsets[(int)lod]);
                    }
                }
            }
        }

        private static void DestroyGeometryResource(uint id)
        {
            lock (geometryMutex)
            {
                IntPtr pointer = geometryHierarchies[id];
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

                geometryHierarchies.Remove(id);
            }
        }
        private static IdType GpuIdFromFakePointer(IntPtr pointer)
        {
            Debug.Assert((pointer & SingleMeshMarker) != 0);
            return (IdType)((pointer >> shiftBits) & IdDetail.InvalidId);
        }

        private static IdType CreateMaterialResource(IntPtr data)
        {
            Debug.Assert(data != IntPtr.Zero);
            var info = Marshal.PtrToStructure<MaterialInitInfo>(data);
            return Renderer.AddMaterial(info);
        }
        private static void DestroyMaterialResource(IdType id)
        {
            Renderer.RemoveMaterial(id);
        }

        public static IdType AddShaderGroup(CompiledShader[] shaders, uint[] keys)
        {
            Debug.Assert(shaders?.Length > 0 && keys?.Length > 0);
            Dictionary<uint, CompiledShader> group = [];
            for (uint i = 0; i < shaders.Length; i++)
            {
                group[keys[i]] = shaders[i];
            }

            lock (shaderMutex)
            {
                return shaderGroups.Add(group);
            }
        }
        public static void RemoveShaderGroup(IdType id)
        {
            lock (shaderMutex)
            {
                Debug.Assert(IdDetail.IsValid(id));
                shaderGroups[id].Clear();
                shaderGroups.Remove(id);
            }
        }
        public static CompiledShader GetShader(IdType id, uint shaderKey)
        {
            lock (shaderMutex)
            {
                Debug.Assert(IdDetail.IsValid(id));

                if (shaderGroups[id].TryGetValue(shaderKey, out var shader))
                {
                    return shader;
                }

                Debug.Assert(false); // should never occure.
                return default;
            }
        }
    }
}
