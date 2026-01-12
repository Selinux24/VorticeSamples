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
        static readonly FreeList<ID3D12Resource> submeshBuffers = new();
        static readonly FreeList<SubmeshView> submeshViews = new();
        static readonly Lock submeshMutex = new();

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
        /// <returns>Returns the submesh id</returns>
        /// <remarks>
        /// NOTE: Expects 'data' to contain:
        /// 
        ///     u32 element_size, u32 vertex_count,
        ///     u32 index_count, u32 elements_type, u32 primitive_topology
        ///     u8 positions[sizeof(f32) * 3 * vertex_count],     // sizeof(positions) must be a multiple of 4 bytes. Pad if needed.
        ///     u8 elements[sizeof(element_size) * vertex_count], // sizeof(elements) must be a multiple of 4 bytes. Pad if needed.
        ///     u8 indices[index_size * index_count],
        /// 
        /// Remarks:
        /// - Advances the data pointer
        /// - Position and element buffers should be padded to be a multiple of 4 bytes in length.
        ///   This 16 bytes is defined as D3D12_STANDARD_MAXIMUM_ELEMENT_ALIGNMENT_BYTE_MULTIPLE.
        /// </remarks>
        public static uint Add(ref IntPtr data)
        {
            BlobStreamReader blob = new(data);

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

            uint alignment = D3D12.StandardMaximumElementAlignmentByteMultiple;
            uint alignedPositionBufferSize = Vortice.Mathematics.MathHelper.AlignUp(positionBufferSize, alignment);
            uint alignedElementBufferSize = Vortice.Mathematics.MathHelper.AlignUp(elementBufferSize, alignment);
            uint totalBufferSize = alignedPositionBufferSize + alignedElementBufferSize + indexBufferSize;

            IntPtr buffer = blob.Position;
            blob.Skip(totalBufferSize);
            var resource = D3D12Helpers.CreateBuffer(buffer, totalBufferSize);

            data = blob.Position;

            SubmeshView view = new();
            view.PositionBufferView.BufferLocation = resource.GPUVirtualAddress;
            view.PositionBufferView.SizeInBytes = positionBufferSize;
            view.PositionBufferView.StrideInBytes = (uint)Marshal.SizeOf<Vector3>();

            if (elementSize > 0)
            {
                view.ElementBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize;
                view.ElementBufferView.SizeInBytes = elementBufferSize;
                view.ElementBufferView.StrideInBytes = elementSize;
            }

            view.IndexBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize + alignedElementBufferSize;
            view.IndexBufferView.SizeInBytes = indexBufferSize;
            view.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
            view.PrimitiveTopology = GetD3DPrimitiveTopology((LibPrimitiveTopology)primitiveTopology);
            view.ElementsType = elementsType;

            lock (submeshMutex)
            {
                submeshBuffers.Add(resource);
                return submeshViews.Add(view);
            }
        }
        /// <summary>
        /// Removes the submesh from the list.
        /// </summary>
        /// <param name="id">Submesh id</param>
        public static void Remove(uint id)
        {
            lock (submeshMutex)
            {
                submeshViews.Remove(id);

                D3D12Graphics.DeferredRelease(submeshBuffers[id]);
                submeshBuffers.Remove(id);
            }
        }
        public static void GetViews(uint[] gpuIds, ref ViewsCache cache)
        {
            Debug.Assert(gpuIds != null);
            uint idCount = (uint)gpuIds.Length;
            Debug.Assert(idCount > 0);

            lock (submeshMutex)
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
