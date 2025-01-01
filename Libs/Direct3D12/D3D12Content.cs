using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    static class D3D12Content
    {
        public struct PositionView
        {
            public VertexBufferView PositionBufferView;
            public IndexBufferView IndexBufferView;
        };

        public struct ElementView
        {
            public VertexBufferView ElementBufferView;
            public uint ElementsType;
            public PrimitiveTopology PrimitiveTopology;
        };

        static readonly List<ID3D12Resource> submeshBuffers = [];
        static readonly List<PositionView> positionViews = [];
        static readonly List<ElementView> elementViews = [];
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
        // struct{
        //     u32 element_size, u32 vertex_count,
        //     u32 index_count, u32 elements_type, u32 primitive_topology
        //     u8 positions[sizeof(f32) * 3 * vertex_count],     // sizeof(positions) must be a multiple of 16 bytes. Pad if needed.
        //     u8 elements[sizeof(element_size) * vertex_count], // sizeof(elements) must be a multiple of 16 bytes. Pad if needed.
        //     u8 indices[index_size * index_count],
        // } submeshes[submesh_count]
        //
        // Remarks:
        // - Advances the data pointer
        // - Position and element buffers should be padded to be a multiple of 16 bytes in length.
        //   This 16 bytes is defined as D3D12_RAW_UAV_SRV_BYTE_ALIGNMENT.
        public static int AddSubmesh(Stream data)
        {
            using BinaryReader blob = new(data);

            uint elementSize = blob.ReadUInt32();
            uint vertexCount = blob.ReadUInt32();
            uint elementsType = blob.ReadUInt32();
            uint primitiveTopology = blob.ReadUInt32();
            uint indexCount = blob.ReadUInt32();
            uint indexSize = (uint)((vertexCount < (1 << 16)) ? sizeof(ushort) : sizeof(uint));

            // NOTE: element size may be 0, for position-only vertex formats.
            uint positionBufferSize = (uint)Marshal.SizeOf(typeof(Vector3)) * vertexCount;
            uint elementBufferSize = elementSize * vertexCount;
            uint indexBufferSize = indexSize * vertexCount;

            uint alignment = D3D12.RawUnorderedAccessViewShaderResourceViewByteAlignment;
            uint alignedPositionBufferSize = Vortice.Mathematics.MathHelper.AlignUp(positionBufferSize, alignment);
            uint alignedElementBufferSize = Vortice.Mathematics.MathHelper.AlignUp(elementBufferSize, alignment);
            uint totalBufferSize = alignedPositionBufferSize + alignedElementBufferSize + indexBufferSize;

            ID3D12Resource resource = D3D12Helpers.CreateBuffer(blob.ReadBytes((int)totalBufferSize), totalBufferSize);

            PositionView positionView = new();
            positionView.PositionBufferView.BufferLocation = resource.GPUVirtualAddress;
            positionView.PositionBufferView.SizeInBytes = (int)positionBufferSize;
            positionView.PositionBufferView.StrideInBytes = Marshal.SizeOf(typeof(Vector3));

            positionView.IndexBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize + alignedElementBufferSize;
            positionView.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
            positionView.IndexBufferView.SizeInBytes = (int)positionBufferSize;

            ElementView elementView = new();
            if (elementSize > 0)
            {
                elementView.ElementBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize;
                elementView.ElementBufferView.SizeInBytes = (int)elementBufferSize;
                elementView.ElementBufferView.StrideInBytes = (int)elementSize;
            }

            elementView.ElementsType = elementsType;
            elementView.PrimitiveTopology = GetD3DPrimitiveTopology((PrimalLike.Content.PrimitiveTopology)primitiveTopology);

            lock (submeshMutex)
            {
                submeshBuffers.Add(resource);
                positionViews.Add(positionView);
                elementViews.Add(elementView);
                return elementViews.Count - 1;
            }
        }

        public static void Remove(int id)
        {
            lock (submeshMutex)
            {
                positionViews[id] = default;
                elementViews[id] = default;

                D3D12Graphics.DeferredRelease(submeshBuffers[id]);
                submeshBuffers[id] = null;
            }
        }
    }
}
