using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Utilities;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12.Content
{
    using D3DPrimitiveTopology = Vortice.Direct3D.PrimitiveTopology;
    using LibPrimitiveTopology = PrimalLike.Graphics.PrimitiveTopology;

    static class Submesh
    {
        static readonly FreeList<ID3D12Resource> meshBuffers = new();
        static readonly FreeList<SubmeshView> submeshViews = new();
        static readonly Lock meshMutex = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {

        }

        /// <summary>
        /// Add submesh to the list.
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="ids">Returns the array of submesh ids</param>
        /// <remarks>
        /// NOTE: Expects 'data' to contain:
        /// 
        /// u32 size_of_mesh_data
        /// struct{
        ///     u8 positions[sizeof(f32) * 3 * vertex_count],     // sizeof(positions) must be a multiple of 4 bytes. Pad if needed.
        ///     u8 elements[elements_size * vertex_count],        // sizeof(elements) must be a multiple of 4 bytes. Pad if needed.
        ///     u8 indices[index_size * index_count],             // sizeof(indices) must be a multiple of 4 bytes. Pad if needed.
        /// } submesh_data[total_submesh_count]
        /// 
        /// struct{
        ///     u32 elements_size, u32 vertex_count,
        ///     u32 index_count, u32 elements_type, u32 primitive_topology
        /// } submesh_info[total_submesh_count]
        ///
        /// Remarks:
        /// - Position and element buffers should be padded to be a multiple of 4 bytes in length.
        ///   This 16 bytes is defined as D3D12_STANDARD_MAXIMUM_ELEMENT_ALIGNMENT_BYTE_MULTIPLE.
        /// </remarks>
        public static uint[] Add(ref IntPtr data, uint count)
        {
            Debug.Assert(data != IntPtr.Zero);
            Debug.Assert(count > 0);

            uint[] ids = new uint[count];
            Array.Fill(ids, uint.MaxValue);

            BlobStreamReader blob = new(data);
            uint meshDataSize = blob.Read<uint>();

            var resource = D3D12Helpers.CreateBuffer(blob.Position, meshDataSize);
            var address = resource.GPUVirtualAddress;
            blob.Skip(meshDataSize); // skip the mesh data to read submesh info.

            lock (meshMutex)
            {
                uint resourceId = meshBuffers.Add(resource);
                uint alignment = D3D12.StandardMaximumElementAlignmentByteMultiple;

                for (uint i = 0; i < ids.Length; i++)
                {
                    uint elementSize = blob.Read<uint>();
                    uint vertexCount = blob.Read<uint>();
                    uint indexCount = blob.Read<uint>();
                    uint elementsType = blob.Read<uint>();
                    uint primitiveTopology = blob.Read<uint>();
                    uint indexSize = (uint)((vertexCount < (1 << 16)) ? sizeof(ushort) : sizeof(uint));

                    // NOTE: element size may be 0, for position-only vertex formats.
                    uint positionBufferSize = (uint)Marshal.SizeOf<Vector3>() * vertexCount;
                    uint elementBufferSize = elementSize * vertexCount;
                    uint indexBufferSize = indexSize * indexCount;

                    uint alignedPositionBufferSize = Vortice.Mathematics.MathHelper.AlignUp(positionBufferSize, alignment);
                    uint alignedElementBufferSize = Vortice.Mathematics.MathHelper.AlignUp(elementBufferSize, alignment);
                    uint alignedIndexBufferSize = Vortice.Mathematics.MathHelper.AlignUp(indexBufferSize, alignment);

                    SubmeshView view = new();
                    view.PositionBufferView.BufferLocation = address;
                    view.PositionBufferView.SizeInBytes = positionBufferSize;
                    view.PositionBufferView.StrideInBytes = (uint)Marshal.SizeOf<Vector3>();

                    if (elementSize > 0)
                    {
                        view.ElementBufferView.BufferLocation = address + alignedPositionBufferSize;
                        view.ElementBufferView.SizeInBytes = elementBufferSize;
                        view.ElementBufferView.StrideInBytes = elementSize;
                    }

                    view.IndexBufferView.BufferLocation = address + alignedPositionBufferSize + alignedElementBufferSize;
                    view.IndexBufferView.SizeInBytes = indexBufferSize;
                    view.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
                    view.PrimitiveTopology = GetD3DPrimitiveTopology((LibPrimitiveTopology)primitiveTopology);
                    view.ElementsType = elementsType;
                    view.ResourceIndex = resourceId;

                    ids[i] = submeshViews.Add(view);

                    address += alignedPositionBufferSize + alignedElementBufferSize + alignedIndexBufferSize;
                }
            }

            return ids;
        }
        /// <summary>
        /// Removes the submesh from the list.
        /// </summary>
        /// <param name="ids">Array of submesh ids</param>
        public static void Remove(uint[] ids, uint count)
        {
            Debug.Assert(count > 0);
            Debug.Assert(ids != null && ids.Length >= count);

            lock (meshMutex)
            {
                uint resourceId = submeshViews[ids[0]].ResourceIndex;
                Debug.Assert(resourceId != uint.MaxValue);

                for (uint i = 0; i < count; i++)
                {
                    submeshViews.Remove(ids[i]);
                }

                D3D12Graphics.DeferredRelease(meshBuffers[resourceId]);
                meshBuffers.Remove(resourceId);
            }
        }
        public static void GetViews(uint[] gpuIds, ref ViewsCache cache)
        {
            Debug.Assert(gpuIds != null);
            uint idCount = (uint)gpuIds.Length;
            Debug.Assert(idCount > 0);

            lock (meshMutex)
            {
                cache.PositionBuffers = new ulong[idCount];
                cache.ElementBuffers = new ulong[idCount];
                cache.IndexBufferViews = new IndexBufferView[idCount];
                cache.PrimitiveTopologies = new D3DPrimitiveTopology[idCount];
                cache.ElementsTypes = new uint[idCount];

                for (uint i = 0; i < idCount; i++)
                {
                    var view = submeshViews[gpuIds[i]];

                    cache.PositionBuffers[i] = view.PositionBufferView.BufferLocation;
                    cache.ElementBuffers[i] = view.ElementBufferView.BufferLocation;
                    cache.IndexBufferViews[i] = view.IndexBufferView;
                    cache.PrimitiveTopologies[i] = view.PrimitiveTopology;
                    cache.ElementsTypes[i] = view.ElementsType;
                }
            }
        }

        static D3DPrimitiveTopology GetD3DPrimitiveTopology(LibPrimitiveTopology type)
        {
            return type switch
            {
                LibPrimitiveTopology.PointList => D3DPrimitiveTopology.PointList,
                LibPrimitiveTopology.LineList => D3DPrimitiveTopology.LineList,
                LibPrimitiveTopology.LineStrip => D3DPrimitiveTopology.LineStrip,
                LibPrimitiveTopology.TriangleList => D3DPrimitiveTopology.TriangleList,
                LibPrimitiveTopology.TriangleStrip => D3DPrimitiveTopology.TriangleStrip,
                _ => D3DPrimitiveTopology.Undefined,
            };
        }
    }
}
