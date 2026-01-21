using Direct3D12.Content;
using Direct3D12.Delight;
using Direct3D12.Lights;
using PrimalLike.Components;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;
using D3D12PrimitiveTopology = Vortice.Direct3D.PrimitiveTopology;

namespace Direct3D12
{
    static class D3D12GPass
    {
        #region Structures

        struct GPassCache()
        {
            private byte[] buffer = [];

            public uint[] D3D12RenderItemIds = [];
            public uint DescriptorIndexCount = 0;

            // NOTE: when adding new arrays, make sure to update resize() and struct_size.
            public uint[] EntityIds = null;
            public uint[] SubmeshGpuIds = null;
            public uint[] MaterialIds = null;
            public ID3D12PipelineState[] GPassPipelineStates = null;
            public ID3D12PipelineState[] DepthPipelineStates = null;
            public ID3D12RootSignature[] RootSignatures = null;
            public MaterialTypes[] MaterialTypes = null;
            public uint[][] DescriptorIndices = null;
            public uint[] TextureCounts = null;
            MaterialSurface[] MaterialSurfaces = null;
            public ulong[] PositionBuffers = null;
            public ulong[] ElementBuffers = null;
            public IndexBufferView[] IndexBufferViews = null;
            public D3D12PrimitiveTopology[] PrimitiveTopologies = null;
            public uint[] ElementsTypes = null;
            public ulong[] PerObjectData = null;
            public ulong[] SrvIndices = null;

            static readonly int StructSize =
                sizeof(uint) +                      // entity_ids
                sizeof(uint) +                      // submesh_gpu_ids
                sizeof(uint) +                      // material_ids
                Marshal.SizeOf<IntPtr>() +          // gpass_pipeline_states
                Marshal.SizeOf<IntPtr>() +          // depth_pipeline_states
                Marshal.SizeOf<IntPtr>() +          // root_signatures
                sizeof(MaterialTypes) +             // material_types
                sizeof(uint) +                      // TODO: descriptor_indices
                sizeof(uint) +                      // texture_counts
                Marshal.SizeOf<MaterialSurface>() + // material_surface
                sizeof(ulong) +                     // position_buffers
                sizeof(ulong) +                     // element_buffers
                Marshal.SizeOf<IndexBufferView>() + // index_buffer_views
                sizeof(D3D12PrimitiveTopology) +    // primitive_topologies
                sizeof(uint) +                      // elements_types
                sizeof(ulong) +                     // per_object_data
                sizeof(ulong);                      // srv_indices

            public readonly uint Size()
            {
                return (uint)D3D12RenderItemIds.Length;
            }
            public void Clear()
            {
                Array.Resize(ref D3D12RenderItemIds, 0);
                DescriptorIndexCount = 0;
            }
            public void Resize()
            {
                ulong itemsCount = (ulong)D3D12RenderItemIds.Length;
                ulong newBufferSize = itemsCount * (ulong)StructSize;
                ulong oldBufferSize = (ulong)buffer.Length;

                if (newBufferSize == oldBufferSize) return;

                Array.Resize(ref buffer, (int)newBufferSize);

                EntityIds = new uint[itemsCount];
                SubmeshGpuIds = new uint[itemsCount];
                MaterialIds = new uint[itemsCount];
                GPassPipelineStates = new ID3D12PipelineState[itemsCount];
                DepthPipelineStates = new ID3D12PipelineState[itemsCount];
                RootSignatures = new ID3D12RootSignature[itemsCount];
                MaterialTypes = new MaterialTypes[itemsCount];
                DescriptorIndices = new uint[itemsCount][];
                TextureCounts = new uint[itemsCount];
                MaterialSurfaces = new MaterialSurface[itemsCount];
                PositionBuffers = new ulong[itemsCount];
                ElementBuffers = new ulong[itemsCount];
                IndexBufferViews = new IndexBufferView[itemsCount];
                PrimitiveTopologies = new D3D12PrimitiveTopology[itemsCount];
                ElementsTypes = new uint[itemsCount];
                PerObjectData = new ulong[itemsCount];
                SrvIndices = new ulong[itemsCount];
            }

            public ItemsCache ItemsCache()
            {
                return new()
                {
                    EntityIds = EntityIds,
                    SubmeshGpuIds = SubmeshGpuIds,
                    MaterialIds = MaterialIds,
                    GPassPsos = GPassPipelineStates,
                    DepthPsos = DepthPipelineStates,
                };
            }
            public ViewsCache ViewsCache()
            {
                return new()
                {
                    PositionBuffers = PositionBuffers,
                    ElementBuffers = ElementBuffers,
                    IndexBufferViews = IndexBufferViews,
                    PrimitiveTopologies = PrimitiveTopologies,
                    ElementsTypes = ElementsTypes
                };
            }
            public MaterialsCache MaterialsCache()
            {
                return new()
                {
                    RootSignatures = RootSignatures,
                    MaterialTypes = MaterialTypes,
                    DescriptorIndices = DescriptorIndices,
                    TextureCounts = TextureCounts,
                    MaterialSurfaces = MaterialSurfaces
                };
            }

            public readonly void SetItems(ItemsCache itemsCache)
            {
                Array.Copy(itemsCache.EntityIds, EntityIds, itemsCache.EntityIds.Length);
                Array.Copy(itemsCache.SubmeshGpuIds, SubmeshGpuIds, itemsCache.SubmeshGpuIds.Length);
                Array.Copy(itemsCache.MaterialIds, MaterialIds, itemsCache.MaterialIds.Length);
                Array.Copy(itemsCache.GPassPsos, GPassPipelineStates, itemsCache.GPassPsos.Length);
                Array.Copy(itemsCache.DepthPsos, DepthPipelineStates, itemsCache.DepthPsos.Length);
            }
            public readonly void SetViews(ViewsCache viewsCache)
            {
                Array.Copy(viewsCache.PositionBuffers, PositionBuffers, viewsCache.PositionBuffers.Length);
                Array.Copy(viewsCache.ElementBuffers, ElementBuffers, viewsCache.ElementBuffers.Length);
                Array.Copy(viewsCache.IndexBufferViews, IndexBufferViews, viewsCache.IndexBufferViews.Length);
                Array.Copy(viewsCache.PrimitiveTopologies, PrimitiveTopologies, viewsCache.PrimitiveTopologies.Length);
                Array.Copy(viewsCache.ElementsTypes, ElementsTypes, viewsCache.ElementsTypes.Length);
            }
            public readonly void SetMaterials(MaterialsCache materialsCache)
            {
                Array.Copy(materialsCache.RootSignatures, RootSignatures, materialsCache.RootSignatures.Length);
                Array.Copy(materialsCache.MaterialTypes, MaterialTypes, materialsCache.MaterialTypes.Length);
            }
        }

        #endregion

        public const Format MainBufferFormat = Format.R16G16B16A16_Float;
        public const Format DepthBufferFormat = Format.D32_Float;

        const uint initialDimensionWidth = 100;
        const uint initialDimensionHeight = 100;

        static D3D12RenderTexture gpassMainBuffer;
        static D3D12DepthBuffer gpassDepthBuffer;
        static uint dimensionWidth = initialDimensionWidth;
        static uint dimensionHeight = initialDimensionHeight;

        static GPassCache frameCache = new();

#if DEBUG
        static readonly Color clearValue = new(0.5f, 0.5f, 0.5f, 1.0f);
#else
        static readonly Color clearValue = new(0.0f);
#endif

        /// <summary>
        /// Main buffer for the G-Buffer pass.
        /// </summary>
        public static D3D12RenderTexture MainBuffer { get => gpassMainBuffer; }
        /// <summary>
        /// Depth buffer for the G-Buffer pass.
        /// </summary>
        public static D3D12DepthBuffer DepthBuffer { get => gpassDepthBuffer; }

        public static bool Initialize()
        {
            return CreateBuffers(initialDimensionWidth, initialDimensionHeight);
        }
        static bool CreateBuffers(uint width, uint height)
        {
            Debug.Assert(width > 0 && height > 0);
            gpassMainBuffer?.Dispose();
            gpassDepthBuffer?.Dispose();

            // Create the main buffer
            D3D12TextureInitInfo gpassMainBufferInfo = new()
            {
                Desc = new()
                {
                    Alignment = 0, // NOTE: 0 is the same as 64KB (or 4MB for MSAA)
                    DepthOrArraySize = 1,
                    Dimension = ResourceDimension.Texture2D,
                    Flags = ResourceFlags.AllowRenderTarget,
                    Format = MainBufferFormat,
                    Height = height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = 0, // make space for all mip levels
                    SampleDescription = new(1, 0),
                    Width = width
                },
                InitialState = ResourceStates.PixelShaderResource,
                ClearValue = new(MainBufferFormat, clearValue),
            };
            gpassMainBuffer = new(gpassMainBufferInfo);
            D3D12Helpers.NameD3D12Object(gpassMainBuffer.GetResource(), "GPass Main Buffer");

            // Create the depth buffer
            D3D12TextureInitInfo gpassDepthBufferInfo = new()
            {
                Desc = ResourceDescription.Texture2D(DepthBufferFormat, width, height, 1, 1, flags: ResourceFlags.AllowDepthStencil),
                InitialState = ResourceStates.DepthRead | ResourceStates.NonPixelShaderResource,
                ClearValue = new(DepthBufferFormat, 0f, 0),
            };
            gpassDepthBuffer = new(gpassDepthBufferInfo);
            D3D12Helpers.NameD3D12Object(gpassDepthBuffer.GetResource(), "GPass Depth Buffer");

            return gpassMainBuffer.GetResource() != null && gpassDepthBuffer.GetResource() != null;
        }

        public static void Shutdown()
        {
            gpassMainBuffer?.Dispose();
            gpassMainBuffer = null;
            gpassDepthBuffer?.Dispose();
            gpassDepthBuffer = null;
            dimensionWidth = initialDimensionWidth;
            dimensionHeight = initialDimensionHeight;
        }

        public static void SetSize(uint width, uint height)
        {
            if (width <= dimensionWidth && height <= dimensionHeight)
            {
                return;
            }

            dimensionWidth = Math.Max(width, dimensionWidth);
            dimensionHeight = Math.Max(height, dimensionHeight);

            CreateBuffers(dimensionWidth, dimensionHeight);
        }

        static void SetRootParameters(ID3D12GraphicsCommandList cmdList, uint cacheIndex)
        {
            Debug.Assert(cacheIndex < frameCache.Size());

            MaterialTypes mtlType = frameCache.MaterialTypes[cacheIndex];
            switch (mtlType)
            {
                case MaterialTypes.Opaque:
                {
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.PositionBuffer, frameCache.PositionBuffers[cacheIndex]);
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.ElementBuffer, frameCache.ElementBuffers[cacheIndex]);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.PerObjectData, frameCache.PerObjectData[cacheIndex]);
                    if (frameCache.TextureCounts[cacheIndex] > 0)
                    {
                        cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.SrvIndices, frameCache.SrvIndices[cacheIndex]);
                    }
                }
                break;
            }
        }

        public static void AddTransitionsForDepthPrePass(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.GetResource(),
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.BeginOnly);

            barriers.AddTransitionBarrier(
                gpassDepthBuffer.GetResource(),
                ResourceStates.DepthRead | ResourceStates.NonPixelShaderResource,
                ResourceStates.DepthWrite);
        }
        public static void SetRenderTargetsForDepthPrePass(ID3D12GraphicsCommandList cmdList)
        {
            var dsv = gpassDepthBuffer.GetDsv();
            cmdList.ClearDepthStencilView(dsv.Cpu, ClearFlags.Depth | ClearFlags.Stencil, 0f, 0, 0, null);
            cmdList.OMSetRenderTargets([], dsv.Cpu);
        }
        public static void DepthPrePass(ID3D12GraphicsCommandList cmdList, ref D3D12FrameInfo d3d12Info)
        {
            PrepareRenderFrame(ref d3d12Info);

            uint itemsCount = frameCache.Size();

            ID3D12RootSignature currentRootSignature = null;
            ID3D12PipelineState currentPipelineState = null;

            for (uint i = 0; i < itemsCount; i++)
            {
                if (currentRootSignature != frameCache.RootSignatures[i])
                {
                    currentRootSignature = frameCache.RootSignatures[i];
                    cmdList.SetGraphicsRootSignature(currentRootSignature);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.GlobalShaderData, d3d12Info.GlobalShaderData);
                }

                if (currentPipelineState != frameCache.DepthPipelineStates[i])
                {
                    currentPipelineState = frameCache.DepthPipelineStates[i];
                    cmdList.SetPipelineState(currentPipelineState);
                }

                SetRootParameters(cmdList, i);

                IndexBufferView ibv = frameCache.IndexBufferViews[i];
                uint indexCount = ibv.SizeInBytes >> (ibv.Format == Format.R16_UInt ? 1 : 2);

                cmdList.IASetIndexBuffer(ibv);
                cmdList.IASetPrimitiveTopology(frameCache.PrimitiveTopologies[i]);
                cmdList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
            }
        }
        static void PrepareRenderFrame(ref D3D12FrameInfo d3d12Info)
        {
            Debug.Assert(d3d12Info.Camera != null);

            frameCache.Clear();
            RenderItem.GetD3D12RenderItemIds(ref d3d12Info.FrameInfo, ref frameCache.D3D12RenderItemIds);
            frameCache.Resize();

            Debug.Assert(d3d12Info.FrameInfo.RenderItemIds != null && d3d12Info.FrameInfo.RenderItemCount > 0);

            var itemsCache = frameCache.ItemsCache();
            RenderItem.GetItems(frameCache.D3D12RenderItemIds, ref itemsCache);
            frameCache.SetItems(itemsCache);

            var viewsCache = frameCache.ViewsCache();
            Submesh.GetViews(itemsCache.SubmeshGpuIds, ref viewsCache);
            frameCache.SetViews(viewsCache);

            var materialsCache = frameCache.MaterialsCache();
            Material.GetMaterials(itemsCache.MaterialIds, ref materialsCache, ref frameCache.DescriptorIndexCount);
            frameCache.SetMaterials(materialsCache);

            FillPerObjectData(ref d3d12Info, materialsCache);

            if (frameCache.DescriptorIndexCount == 0)
            {
                return;
            }

            var cbuffer = D3D12Graphics.CBuffer;

            uint itemsCount = frameCache.Size();
            for (uint i = 0; i < itemsCount; i++)
            {
                frameCache.SrvIndices[i] = 0;

                if (frameCache.TextureCounts[i] == 0)
                {
                    continue;
                }

                var gpuAddress = cbuffer.Write(frameCache.DescriptorIndices[i]);
                frameCache.SrvIndices[i] = gpuAddress;
            }
        }
        static void FillPerObjectData(ref D3D12FrameInfo d3d12Info, MaterialsCache materialsCache)
        {
            uint renderItemsCount = frameCache.Size();
            uint currentEntityId = uint.MaxValue;
            ulong currentGpuAddress = 0;

            for (uint i = 0; i < renderItemsCount; i++)
            {
                if (currentEntityId != frameCache.EntityIds[i])
                {
                    currentEntityId = frameCache.EntityIds[i];
                    Transform.GetTransformMatrices(currentEntityId, out var world, out var invWorld);
                    var wvp = Matrix4x4.Multiply(world, d3d12Info.Camera.ViewProjection);

                    var surface = materialsCache.MaterialSurfaces[i];

                    Shaders.PerObjectData data = new()
                    {
                        World = world,
                        InvWorld = invWorld,
                        WorldViewProjection = wvp,

                        BaseColor = surface.BaseColor,
                        Emissive = surface.Emissive,
                        EmissiveIntensity = surface.EmissiveIntensity,
                        Metallic = surface.Metallic,
                        Roughness = surface.Roughness,
                    };

                    currentGpuAddress = D3D12Graphics.CBuffer.Write(data);
                }

                Debug.Assert(currentGpuAddress != 0);
                frameCache.PerObjectData[i] = currentGpuAddress;
            }
        }

        public static void AddTransitionsForGPass(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.GetResource(),
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.EndOnly);

            barriers.AddTransitionBarrier(
                gpassDepthBuffer.GetResource(),
                ResourceStates.DepthWrite,
                ResourceStates.DepthRead | ResourceStates.NonPixelShaderResource);
        }
        public static void SetRenderTargetsForGPass(ID3D12GraphicsCommandList cmdList)
        {
            var rtv = gpassMainBuffer.GetRtv(0);
            var dsv = gpassDepthBuffer.GetDsv();

            cmdList.ClearRenderTargetView(rtv.Cpu, clearValue, 0, null);
            cmdList.OMSetRenderTargets(rtv.Cpu, dsv.Cpu);
        }
        public static void Render(ID3D12GraphicsCommandList cmdList, ref D3D12FrameInfo d3d12Info)
        {
            uint itemsCount = frameCache.Size();
            uint frameIndex = d3d12Info.FrameIndex;
            uint lightCullingId = d3d12Info.LightCullingId;

            ID3D12RootSignature currentRootSignature = null;
            ID3D12PipelineState currentPipelineState = null;

            for (uint i = 0; i < itemsCount; i++)
            {
                if (currentRootSignature != frameCache.RootSignatures[i])
                {
                    currentRootSignature = frameCache.RootSignatures[i];
                    cmdList.SetGraphicsRootSignature(currentRootSignature);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.GlobalShaderData, d3d12Info.GlobalShaderData);
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.DirectionalLights, D3D12Light.NonCullableLightBuffer(frameIndex));
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.CullableLights, D3D12Light.CullableLightBuffer(frameIndex));
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.LightGrid, D3D12LightCulling.LightGridOpaque(lightCullingId, frameIndex));
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.LightIndexList, D3D12LightCulling.LightIndexListOpaque(lightCullingId, frameIndex));
                }

                if (currentPipelineState != frameCache.GPassPipelineStates[i])
                {
                    currentPipelineState = frameCache.GPassPipelineStates[i];
                    cmdList.SetPipelineState(currentPipelineState);
                }

                SetRootParameters(cmdList, i);

                var ibv = frameCache.IndexBufferViews[i];
                uint indexCount = ibv.SizeInBytes >> (ibv.Format == Format.R16_UInt ? 1 : 2);

                cmdList.IASetIndexBuffer(ibv);
                cmdList.IASetPrimitiveTopology(frameCache.PrimitiveTopologies[i]);
                cmdList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
            }
        }

        public static void AddTransitionsForPostProcess(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.GetResource(),
                ResourceStates.RenderTarget,
                ResourceStates.PixelShaderResource);
        }
    }
}
