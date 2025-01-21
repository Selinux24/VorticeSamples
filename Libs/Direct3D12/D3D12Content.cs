using Direct3D12.Materials;
using PrimalLike.Common;
using PrimalLike.Content;
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
    using D3DPrimitiveTopologyType = Vortice.Direct3D12.PrimitiveTopologyType;

    static class D3D12Content
    {
        struct PsoId()
        {
            public uint GpassPsoId = uint.MaxValue;
            public uint DepthPsoId = uint.MaxValue;
        }
        struct SubmeshView
        {
            public VertexBufferView PositionBufferView;
            public VertexBufferView ElementBufferView;
            public IndexBufferView IndexBufferView;
            public D3DPrimitiveTopology PrimitiveTopology;
            public uint ElementsType;
        }
        struct D3D12RenderItem
        {
            public uint EntityId;
            public uint SubmeshGpuId;
            public uint MaterialId;
            public uint PsoId;
            public uint DepthPsoId;
        }
        struct FrameCache()
        {
            public List<LodOffset> LodOffsets = [];
            public List<uint> GeometryIds = [];
        }

        public struct ViewsCache
        {
            public ulong[] PositionBuffers;
            public ulong[] ElementBuffers;
            public IndexBufferView[] IndexBufferViews;
            public D3DPrimitiveTopology[] PrimitiveTopologies;
            public uint[] ElementsTypes;
        }
        public struct MaterialsCache
        {
            public ID3D12RootSignature[] RootSignatures;
            public MaterialTypes[] MaterialTypes;
        }
        public struct ItemsCache
        {
            public uint[] EntityIds;
            public uint[] SubmeshGpuIds;
            public uint[] MaterialIds;
            public ID3D12PipelineState[] GPassPsos;
            public ID3D12PipelineState[] DepthPsos;
        }

        static readonly FreeList<ID3D12Resource> submeshBuffers = new();
        static readonly FreeList<SubmeshView> submeshViews = new();
        static readonly object submeshMutex = new();

        static readonly FreeList<D3D12Texture> textures = new();
        static readonly object textureMutex = new();

        static readonly List<ID3D12RootSignature> rootSignatures = [];
        static readonly Dictionary<ulong, uint> mtlRsMap = []; // maps a material's type and shader flags to an index in the array of root signatures.
        static readonly FreeList<IntPtr> materials = new();
        static readonly object materialMutex = new();

        static readonly FreeList<D3D12RenderItem> renderItems = new();
        static readonly FreeList<uint[]> renderItemIds = new();
        static readonly List<ID3D12PipelineState> pipelineStates = [];
        static readonly Dictionary<ulong, uint> psoMap = [];
        static readonly object renderItemMutex = new();

        static readonly FrameCache frameCache = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {
            // NOTE: we only release data that were created as a side-effect to adding resources,
            //       which the user of this module has no control over. The rest of data should be released
            //       by the user, by calling "remove" functions, prior to shutting down the renderer.
            //       That way we make sure the book-keeping of content is correct.

            foreach (var item in rootSignatures)
            {
                item.Dispose();
            }

            mtlRsMap.Clear();
            rootSignatures.Clear();

            foreach (var item in pipelineStates)
            {
                item.Dispose();
            }

            psoMap.Clear();
            pipelineStates.Clear();
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
                    RootParameter1[] parameters = new RootParameter1[(uint)OpaqueRootParameter.Count];
                    parameters[(uint)OpaqueRootParameter.GlobalShaderData] = D3D12Helpers.AsCbv(ShaderVisibility.All, 0);

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

                    parameters[(uint)OpaqueRootParameter.PositionBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 0);
                    parameters[(uint)OpaqueRootParameter.ElementBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 1);
                    parameters[(uint)OpaqueRootParameter.SrvIndices] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 2); // TODO: needs to be visible to any stages that need to sample textures.
                    parameters[(uint)OpaqueRootParameter.PerObjectData] = D3D12Helpers.AsCbv(dataVisibility, 1);

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
            D3D12Helpers.NameD3D12Object(rootSignature, key, "GPass Root Signature - key");

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
        private static D3DPrimitiveTopologyType GetD3DPrimitiveTopologyType(D3DPrimitiveTopology topology)
        {
            return topology switch
            {
                D3DPrimitiveTopology.PointList => D3DPrimitiveTopologyType.Point,
                D3DPrimitiveTopology.LineList or D3DPrimitiveTopology.LineStrip => D3DPrimitiveTopologyType.Line,
                D3DPrimitiveTopology.TriangleList or D3DPrimitiveTopology.TriangleStrip => D3DPrimitiveTopologyType.Triangle,
                _ => D3DPrimitiveTopologyType.Undefined,
            };
        }

        private static uint CreatePsoIfNeeded<T>(T data, bool isDepth) where T : unmanaged
        {
            // calculate Crc32 hash of the data.
            ulong key = (ulong)D3D12Helpers.GetStableHashCode(data);

            if (psoMap.TryGetValue(key, out uint pair))
            {
                return pair;
            }

            uint id = (uint)pipelineStates.Count;
            var pso = D3D12Graphics.Device.CreatePipelineState(data);
            pipelineStates.Add(pso);

            D3D12Helpers.NameD3D12Object(pipelineStates[^1], key, isDepth ? "Depth-only Pipeline State Object - key" : "GPass Pipeline State Object - key");


            Debug.Assert(IdDetail.IsValid(id));
            psoMap[key] = id;
            return id;
        }
        private static PsoId CreatePso(uint materialId, D3DPrimitiveTopology primitiveTopology, uint elementsType)
        {
            lock (materialMutex)
            {
                D3D12MaterialStream material = new(materials[materialId]);

                D3D12PipelineStateSubobjectStream stream = new()
                {
                    RenderTargetFormats = new([D3D12GPass.mainBufferFormat]),
                    RootSignature = rootSignatures[(int)material.RootSignatureId],
                    PrimitiveTopology = GetD3DPrimitiveTopologyType(primitiveTopology),
                    DepthStencilFormat = D3D12GPass.depthBufferFormat,
                    Rasterizer = D3D12Helpers.RasterizerStatesCollection.BackFaceCull,
                    DepthStencil1 = D3D12Helpers.DepthStatesCollection.EnabledReadonly,
                    Blend = D3D12Helpers.BlendStatesCollection.Disabled,
                };

                ShaderFlags flags = material.ShaderFlags;
                CompiledShader[] shaders = new CompiledShader[(int)ShaderTypes.Count];
                uint shaderIndex = 0;
                for (uint i = 0; i < (uint)ShaderTypes.Count; i++)
                {
                    ShaderFlags shaderFlags = (ShaderFlags)(1u << (int)i);
                    if (flags.HasFlag(shaderFlags))
                    {
                        uint shaderId = material.ShaderIds[shaderIndex];
                        var shader = ContentToEngine.GetShader(shaderId);
                        Debug.Assert(shader.ByteCodeSize > 0);
                        shaders[i] = shader;
                        shaderIndex++;
                    }
                }

                stream.Vs = new(shaders[(int)ShaderTypes.Vertex].ByteCode.Span);
                stream.Ps = new(shaders[(int)ShaderTypes.Pixel].ByteCode.Span);
                stream.Ds = new(shaders[(int)ShaderTypes.Domain].ByteCode.Span);
                stream.Hs = new(shaders[(int)ShaderTypes.Hull].ByteCode.Span);
                stream.Gs = new(shaders[(int)ShaderTypes.Geometry].ByteCode.Span);
                stream.Cs = new(shaders[(int)ShaderTypes.Compute].ByteCode.Span);
                stream.As = new(shaders[(int)ShaderTypes.Amplification].ByteCode.Span);
                stream.Ms = new(shaders[(int)ShaderTypes.Mesh].ByteCode.Span);

                uint gPassPsoId = CreatePsoIfNeeded(stream, false);

                stream.Ps = new([]);
                stream.DepthStencil1 = D3D12Helpers.DepthStatesCollection.Enabled;

                uint depthPsoId = CreatePsoIfNeeded(stream, true);

                return new()
                {
                    GpassPsoId = gPassPsoId,
                    DepthPsoId = depthPsoId
                };
            }
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
            view.IndexBufferView.SizeInBytes = indexBufferSize;
            view.IndexBufferView.Format = indexSize == sizeof(ushort) ? Format.R16_UInt : Format.R32_UInt;
            view.PrimitiveTopology = GetD3DPrimitiveTopology((PrimitiveTopology)primitiveTopology);
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
        public static void RemoveSubmesh(uint id)
        {
            lock (submeshMutex)
            {
                submeshViews.Remove(id);

                D3D12Graphics.DeferredRelease(submeshBuffers[id]);
                submeshBuffers.Remove(id);
            }
        }
        public static void GetSubmeshViews(uint[] gpuIds, ref ViewsCache cache)
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
                for (uint i = 0; i < textureIds.Length; i++)
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
                return materials.Add(buffer);
            }
        }
        public static void RemoveMaterial(uint id)
        {
            lock (materialMutex)
            {
                materials.Remove(id);
            }
        }
        public static void GetMaterials(uint[] materialIds, uint materialCount, ref MaterialsCache cache)
        {
            Debug.Assert(materialIds != null && materialCount != 0);
            Debug.Assert(cache.RootSignatures != null && cache.MaterialTypes != null);

            lock (materialMutex)
            {
                for (uint i = 0; i < materialCount; i++)
                {
                    var stream = new D3D12MaterialStream(materials[materialIds[i]]);

                    cache.RootSignatures[i] = rootSignatures[(int)stream.RootSignatureId];
                    cache.MaterialTypes[i] = stream.MaterialType;
                }
            }
        }

        /// <summary>
        /// Creates a buffer that's basically an array of IdType
        /// </summary>
        /// <remarks>
        /// buffer[0] = geometry_content_id
        /// buffer[1 .. n] = d3d12_render_item_ids (n is the number of low-level render item ids which must also equal the number of submeshes/material ids).
        /// buffer[n + 1] = id::invalid_id (this marks the end of submesh_gpu_id array).
        /// </remarks>
        public static uint AddRenderItem(uint entityId, uint geometryContentId, uint[] materialIds)
        {
            Debug.Assert(IdDetail.IsValid(entityId) && IdDetail.IsValid(geometryContentId));
            uint materialCount = (uint)(materialIds?.Length ?? 0);
            Debug.Assert(materialCount > 0);
            ContentToEngine.GetSubmeshGpuIds(geometryContentId, materialCount, out uint[] gpuIds);

            ViewsCache viewsCache = new();
            GetSubmeshViews(gpuIds, ref viewsCache);

            // NOTE: the list of ids starts with geomtery id and ends with an invalid id to mark the end of the list.
            uint[] items = new uint[1 + materialCount + 1];

            items[0] = geometryContentId;

            lock (renderItemMutex)
            {
                for (uint i = 0; i < materialCount; i++)
                {
                    PsoId idPair = CreatePso(materialIds[i], viewsCache.PrimitiveTopologies[i], viewsCache.ElementsTypes[i]);

                    D3D12RenderItem item = new()
                    {
                        EntityId = entityId,
                        SubmeshGpuId = gpuIds[i],
                        MaterialId = materialIds[i],
                        PsoId = idPair.GpassPsoId,
                        DepthPsoId = idPair.DepthPsoId
                    };

                    Debug.Assert(IdDetail.IsValid(item.SubmeshGpuId) && IdDetail.IsValid(item.MaterialId));
                    items[i + 1] = renderItems.Add(item);
                }

                // mark the end of ids list.
                items[materialCount + 1] = IdDetail.InvalidId;

                return renderItemIds.Add(items);
            }
        }
        public static void RemoveRenderItem(uint id)
        {
            lock (renderItemMutex)
            {
                var items = renderItemIds[id];

                // NOTE: the last element in the list of ids is always an invalid id.
                for (uint i = 1; items[i] != IdDetail.InvalidId; i++)
                {
                    renderItems.Remove(items[i]);
                }

                renderItemIds.Remove(id);
            }
        }
        public static void GetD3D12RenderItemIds(ref FrameInfo info, ref uint[] d3d12RenderItemIds)
        {
            Debug.Assert(info.RenderItemIds != null && info.Thresholds != null && info.RenderItemCount != 0);
            Debug.Assert(d3d12RenderItemIds.Length == 0);

            frameCache.LodOffsets.Clear();
            frameCache.GeometryIds.Clear();
            uint count = info.RenderItemCount;

            lock (renderItemMutex)
            {
                for (uint i = 0; i < count; i++)
                {
                    var buffer = renderItemIds[info.RenderItemIds[i]];
                    frameCache.GeometryIds.Add(buffer[0]);
                }

                ContentToEngine.GetLodOffsets([.. frameCache.GeometryIds], info.Thresholds, count, frameCache.LodOffsets);
                Debug.Assert(frameCache.LodOffsets.Count == count);

                uint d3d12RenderItemCount = 0;
                for (uint i = 0; i < count; i++)
                {
                    d3d12RenderItemCount += frameCache.LodOffsets[(int)i].Count;
                }

                Debug.Assert(d3d12RenderItemCount > 0);
                Array.Resize(ref d3d12RenderItemIds, (int)d3d12RenderItemCount);

                uint itemIndex = 0;
                for (uint i = 0; i < count; i++)
                {
                    var items = renderItemIds[info.RenderItemIds[i]];
                    var lodOffset = frameCache.LodOffsets[(int)i];

                    Array.Copy(items, lodOffset.Offset + 1, d3d12RenderItemIds, itemIndex, lodOffset.Count);
                    itemIndex += lodOffset.Count;
                    Debug.Assert(itemIndex <= d3d12RenderItemCount);
                }

                Debug.Assert(itemIndex <= d3d12RenderItemCount);
            }
        }
        public static void GetItems(uint[] d3d12RenderItemIds, uint idCount, ref ItemsCache cache)
        {
            Debug.Assert(d3d12RenderItemIds != null && idCount != 0);
            Debug.Assert(
                cache.EntityIds != null &&
                cache.SubmeshGpuIds != null &&
                cache.MaterialIds != null &&
                cache.GPassPsos != null &&
                cache.DepthPsos != null);

            lock (renderItemMutex)
            {
                for (uint i = 0; i < idCount; i++)
                {
                    var item = renderItems[d3d12RenderItemIds[i]];
                    cache.EntityIds[i] = item.EntityId;
                    cache.SubmeshGpuIds[i] = item.SubmeshGpuId;
                    cache.MaterialIds[i] = item.MaterialId;
                    cache.GPassPsos[i] = pipelineStates[(int)item.PsoId];
                    cache.DepthPsos[i] = pipelineStates[(int)item.DepthPsoId];
                }
            }
        }
    }
}
