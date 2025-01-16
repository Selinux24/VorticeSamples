using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    using D3DPrimitiveTopology = Vortice.Direct3D.PrimitiveTopology;

    static class D3D12Content
    {
        public struct SubmeshView
        {
            public VertexBufferView PositionBufferView;
            public VertexBufferView ElementBufferView;
            public IndexBufferView IndexBufferView;
            public D3DPrimitiveTopology PrimitiveTopology;
            public uint ElementsType;
        };

        static readonly FreeList<ID3D12Resource> submeshBuffers = new();
        static readonly FreeList<SubmeshView> submeshViews = new();
        static readonly object submeshMutex = new();

        static readonly FreeList<D3D12Texture> textures = new();
        static readonly object textureMutex = new();

        static readonly List<ID3D12RootSignature> rootSignatures = [];
        static readonly Dictionary<ulong, uint> mtlRsMap = []; // maps a material's type and shader flags to an index in the array of root signatures.
        static readonly FreeList<IntPtr> materials = new();
        static readonly object materialMutex = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {
            foreach (var item in rootSignatures)
            {
                item.Dispose();
            }
        }

        public static uint CreateRootSignature(MaterialTypes type, ShaderFlags flags)
        {
            Debug.Assert(type < MaterialTypes.Count);
            Debug.Assert(sizeof(MaterialTypes) == sizeof(uint) && sizeof(ShaderFlags) == sizeof(uint));
            ulong key = ((uint)type << 32) | (uint)flags;
            if (mtlRsMap.TryGetValue(key, out uint value))
            {
                return value;
            }

            ID3D12RootSignature rootSignature = null;

            switch (type)
            {
                case MaterialTypes.Opaque:
                {
                    RootParameter1[] parameters = new RootParameter1[(uint)D3D12GPass.OpaqueRootParameter.Count];
                    parameters[(uint)D3D12GPass.OpaqueRootParameter.PerFrameData] = D3D12Helpers.AsCbv(ShaderVisibility.All, 0);

                    ShaderVisibility bufferVisibility = new();
                    ShaderVisibility dataVisibility = new();

                    if (flags.HasFlag(ShaderFlags.Vertex))
                    {
                        bufferVisibility = ShaderVisibility.Vertex;
                        dataVisibility = ShaderVisibility.Vertex;
                    }
                    else if (flags.HasFlag(ShaderFlags.Mesh))
                    {
                        bufferVisibility = ShaderVisibility.Mesh;
                        dataVisibility = ShaderVisibility.Mesh;
                    }

                    if (flags.HasFlag(ShaderFlags.Hull) ||
                        flags.HasFlag(ShaderFlags.Geometry) ||
                        flags.HasFlag(ShaderFlags.Amplification))
                    {
                        bufferVisibility = ShaderVisibility.All;
                        dataVisibility = ShaderVisibility.All;
                    }

                    if (flags.HasFlag(ShaderFlags.Pixel) ||
                        flags.HasFlag(ShaderFlags.Compute))
                    {
                        dataVisibility = ShaderVisibility.All;
                    }

                    parameters[(uint)D3D12GPass.OpaqueRootParameter.PositionBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 0);
                    parameters[(uint)D3D12GPass.OpaqueRootParameter.ElementBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 1);
                    parameters[(uint)D3D12GPass.OpaqueRootParameter.SrvIndices] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 2); // TODO: needs to be visible to any stages that need to sample textures.
                    parameters[(uint)D3D12GPass.OpaqueRootParameter.PerObjectData] = D3D12Helpers.AsCbv(dataVisibility, 1);

                    var rootSignatureDesc = new D3D12RootSignatureDesc(parameters, GetRootSignatureFlags(flags));
                    rootSignature = rootSignatureDesc.Create();
                    Debug.Assert(rootSignature != null);
                }
                break;
            }

            Debug.Assert(rootSignature != null);
            uint id = (uint)rootSignatures.Count;
            rootSignatures.Add(rootSignature);
            mtlRsMap[key] = id;
            D3D12Helpers.NameD3D12Object(rootSignature, (int)key, "GPass Root Signature - key");

            return id;
        }
        private static RootSignatureFlags GetRootSignatureFlags(ShaderFlags flags)
        {
            RootSignatureFlags defaultFlags = D3D12RootSignatureDesc.DefaultFlags;
            if (flags.HasFlag(ShaderFlags.Vertex)) defaultFlags &= ~RootSignatureFlags.DenyVertexShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Hull)) defaultFlags &= ~RootSignatureFlags.DenyHullShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Domain)) defaultFlags &= ~RootSignatureFlags.DenyDomainShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Geometry)) defaultFlags &= ~RootSignatureFlags.DenyGeometryShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Pixel)) defaultFlags &= ~RootSignatureFlags.DenyPixelShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Amplification)) defaultFlags &= ~RootSignatureFlags.DenyAmplificationShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Mesh)) defaultFlags &= ~RootSignatureFlags.DenyMeshShaderRootAccess;
            return defaultFlags;
        }

        private static D3DPrimitiveTopology GetD3DPrimitiveTopology(PrimitiveTopology type)
        {
            return type switch
            {
                PrimitiveTopology.PointList => D3DPrimitiveTopology.PointList,
                PrimitiveTopology.LineList => D3DPrimitiveTopology.LineList,
                PrimitiveTopology.LineStrip => D3DPrimitiveTopology.LineStrip,
                PrimitiveTopology.TriangleList => D3DPrimitiveTopology.TriangleList,
                PrimitiveTopology.TriangleStrip => D3DPrimitiveTopology.TriangleStrip,
                _ => D3DPrimitiveTopology.Undefined,
            };
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
        public static uint AddSubmesh(ref IntPtr data)
        {
            BlobStreamReader blob = new(data);

            uint elementSize = blob.Read<uint>();
            uint vertexCount = blob.Read<uint>();
            uint indexCount = blob.Read<uint>();
            uint elementsType = blob.Read<uint>();
            uint primitiveTopology = blob.Read<uint>();
            uint indexSize = (uint)((vertexCount < (1 << 16)) ? sizeof(ushort) : sizeof(uint));

            // NOTE: element size may be 0, for position-only vertex formats.
            uint positionBufferSize = (uint)Marshal.SizeOf(typeof(Vector3)) * vertexCount;
            uint elementBufferSize = elementSize * vertexCount;
            uint indexBufferSize = indexSize * indexCount;

            uint alignment = D3D12.StandardMaximumElementAlignmentByteMultiple;
            uint alignedPositionBufferSize = Vortice.Mathematics.MathHelper.AlignUp(positionBufferSize, alignment);
            uint alignedElementBufferSize = Vortice.Mathematics.MathHelper.AlignUp(elementBufferSize, alignment);
            uint totalBufferSize = alignedPositionBufferSize + alignedElementBufferSize + indexBufferSize;

            byte[] buffer = blob.Read((int)totalBufferSize);
            var resource = D3D12Helpers.CreateBuffer(buffer, totalBufferSize);

            data = blob.Position;

            SubmeshView view = new();
            view.PositionBufferView.BufferLocation = resource.GPUVirtualAddress;
            view.PositionBufferView.SizeInBytes = positionBufferSize;
            view.PositionBufferView.StrideInBytes = (uint)Marshal.SizeOf(typeof(Vector3));

            if (elementSize > 0)
            {
                view.ElementBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize;
                view.ElementBufferView.SizeInBytes = elementBufferSize;
                view.ElementBufferView.StrideInBytes = elementSize;
            }

            view.IndexBufferView.BufferLocation = resource.GPUVirtualAddress + alignedPositionBufferSize + alignedElementBufferSize;
            view.IndexBufferView.SizeInBytes = positionBufferSize;
            view.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
            view.PrimitiveTopology = GetD3DPrimitiveTopology((PrimitiveTopology)primitiveTopology);
            view.ElementsType = elementsType;

            lock (submeshMutex)
            {
                submeshBuffers.Add(resource);
                return (uint)submeshViews.Add(view);
            }
        }
        /// <summary>
        /// Removes the submesh from the list.
        /// </summary>
        /// <param name="id">Submesh id</param>
        public static void RemoveSubmesh(uint id)
        {
            lock (submeshMutex)
            {
                submeshViews.Remove((int)id);

                D3D12Graphics.DeferredRelease(submeshBuffers[(int)id]);
                submeshBuffers.Remove((int)id);
            }
        }

        public static uint AddTexture(ref IntPtr data)
        {
            return uint.MaxValue;
        }
        public static void RemoveTexture(uint id)
        {

        }
        public static void GetTextureDescriptorIndices(uint[] textureIds, uint[] indices)
        {
            Debug.Assert(textureIds != null && indices != null);
            lock (textureMutex)
            {
                for (int i = 0; i < textureIds.Length; i++)
                {
                    indices[i] = textures[i].Srv.Index;
                }
            }
        }

        public static uint AddMaterial(MaterialInitInfo info)
        {
            IntPtr buffer = IntPtr.Zero;
            lock (materialMutex)
            {
                D3D12MaterialStream stream = new(ref buffer, info);
                Debug.Assert(buffer != IntPtr.Zero);
                return (uint)materials.Add(buffer);
            }
        }
        public static void RemoveMaterial(uint id)
        {
            lock (materialMutex)
            {
                materials.Remove((int)id);
            }
        }
    }
}
