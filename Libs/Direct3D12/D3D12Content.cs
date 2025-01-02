using PrimalLike.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    static class D3D12Content
    {
        public struct SubmeshView
        {
            public VertexBufferView PositionBufferView;
            public VertexBufferView ElementBufferView;
            public IndexBufferView IndexBufferView;
            public PrimitiveTopology PrimitiveTopology;
            public uint ElementsType;
        };

        static readonly List<ID3D12Resource> submeshBuffers = [];
        static readonly List<SubmeshView> submeshViews = [];
        static readonly object submeshMutex = new();

        private static PrimitiveTopology GetD3DPrimitiveTopology(PrimalLike.Content.PrimitiveTopology type)
        {
            return type switch
            {
                PrimalLike.Content.PrimitiveTopology.PointList => PrimitiveTopology.PointList,
                PrimalLike.Content.PrimitiveTopology.LineList => PrimitiveTopology.LineList,
                PrimalLike.Content.PrimitiveTopology.LineStrip => PrimitiveTopology.LineStrip,
                PrimalLike.Content.PrimitiveTopology.TriangleList => PrimitiveTopology.TriangleList,
                PrimalLike.Content.PrimitiveTopology.TriangleStrip => PrimitiveTopology.TriangleStrip,
                _ => PrimitiveTopology.Undefined,
            };
        }

        // NOTE: Expects 'data' to contain:
        // 
        //     u32 element_size, u32 vertex_count,
        //     u32 index_count, u32 elements_type, u32 primitive_topology
        //     u8 positions[sizeof(f32) * 3 * vertex_count],     // sizeof(positions) must be a multiple of 4 bytes. Pad if needed.
        //     u8 elements[sizeof(element_size) * vertex_count], // sizeof(elements) must be a multiple of 4 bytes. Pad if needed.
        //     u8 indices[index_size * index_count],
        //
        // Remarks:
        // - Advances the data pointer
        // - Position and element buffers should be padded to be a multiple of 4 bytes in length.
        //   This 16 bytes is defined as D3D12_STANDARD_MAXIMUM_ELEMENT_ALIGNMENT_BYTE_MULTIPLE.
        public static int AddSubmesh(IntPtr data)
        {
            BlobStreamReader blob = new(data);

            uint elementSize = blob.ReadUInt32();
            uint vertexCount = blob.ReadUInt32();
            uint indexCount = blob.ReadUInt32();
            uint elementsType = blob.ReadUInt32();
            uint primitiveTopology = blob.ReadUInt32();
            uint indexSize = (uint)((vertexCount < (1 << 16)) ? sizeof(ushort) : sizeof(uint));

            // NOTE: element size may be 0, for position-only vertex formats.
            uint positionBufferSize = (uint)Marshal.SizeOf(typeof(Vector3)) * vertexCount;
            uint elementBufferSize = elementSize * vertexCount;
            uint indexBufferSize = indexSize * indexCount;

            uint alignment = D3D12.StandardMaximumElementAlignmentByteMultiple;
            uint alignedPositionBufferSize = Vortice.Mathematics.MathHelper.AlignUp(positionBufferSize, alignment);
            uint alignedElementBufferSize = Vortice.Mathematics.MathHelper.AlignUp(elementBufferSize, alignment);
            uint totalBufferSize = alignedPositionBufferSize + alignedElementBufferSize + indexBufferSize;

            ID3D12Resource resource = D3D12Helpers.CreateBuffer(blob.ReadBytes((int)totalBufferSize), totalBufferSize);

            SubmeshView view = new();
            view.PositionBufferView.BufferLocation = resource.GPUVirtualAddress;
            view.PositionBufferView.SizeInBytes = (int)positionBufferSize;
            view.PositionBufferView.StrideInBytes = Marshal.SizeOf(typeof(Vector3));

            if (elementSize > 0)
            {
                view.ElementBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize;
                view.ElementBufferView.SizeInBytes = (int)elementBufferSize;
                view.ElementBufferView.StrideInBytes = (int)elementSize;
            }

            view.IndexBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize + alignedElementBufferSize;
            view.IndexBufferView.SizeInBytes = (int)positionBufferSize;
            view.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
            view.PrimitiveTopology = GetD3DPrimitiveTopology((PrimalLike.Content.PrimitiveTopology)primitiveTopology);
            view.ElementsType = elementsType;

            lock (submeshMutex)
            {
                submeshBuffers.Add(resource);
                submeshViews.Add(view);
                return submeshViews.Count - 1;
            }
        }

        public static void Remove(int id)
        {
            lock (submeshMutex)
            {
                submeshViews[id] = default;

                D3D12Graphics.DeferredRelease(submeshBuffers[id]);
                submeshBuffers[id] = null;
            }
        }
    }
}
