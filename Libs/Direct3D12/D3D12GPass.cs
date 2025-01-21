using Direct3D12.Materials;
using Direct3D12.Shaders;
using PrimalLike.Components;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Direct3D12
{
    using D3D12PrimitiveTopology = Vortice.Direct3D.PrimitiveTopology;

    static class D3D12GPass
    {
        [StructLayout(LayoutKind.Sequential)]
        struct FrameConstants
        {
            public float Width;
            public float Height;
            public int Frame;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PipelineStateStream
        {
            public PipelineStateSubObjectTypeRootSignature RootSignature;
            public PipelineStateSubObjectTypeVertexShader Vs;
            public PipelineStateSubObjectTypePixelShader Ps;
            public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
            public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
            public PipelineStateSubObjectTypeDepthStencilFormat DepthStencilFormat;
            public PipelineStateSubObjectTypeRasterizer Rasterizer;
            public PipelineStateSubObjectTypeDepthStencil1 Depth;
        }

        struct GPassCache()
        {
            public uint[] d3d12RenderItemIds = [];

            // NOTE: when adding new arrays, make sure to update resize() and struct_size.
            public uint[] entityIds = null;
            public uint[] submeshGpuIds = null;
            public uint[] materialIds = null;
            public ID3D12PipelineState[] gpassPipelineStates = null;
            public ID3D12PipelineState[] depthPipelineStates = null;
            public ID3D12RootSignature[] rootSignatures = null;
            public MaterialTypes[] materialTypes = null;
            public ulong[] positionBuffers = null;
            public ulong[] elementBuffers = null;
            public IndexBufferView[] indexBufferViews = null;
            public D3D12PrimitiveTopology[] primitiveTopologies = null;
            public uint[] elementsTypes = null;
            public ulong[] perObjectData = null;

            public D3D12Content.ItemsCache ItemsCache()
            {
                return new()
                {
                    EntityIds = entityIds,
                    SubmeshGpuIds = submeshGpuIds,
                    MaterialIds = materialIds,
                    GPassPsos = gpassPipelineStates,
                    DepthPsos = depthPipelineStates,
                };
            }

            public D3D12Content.ViewsCache ViewsCache()
            {
                return new()
                {
                    PositionBuffers = positionBuffers,
                    ElementBuffers = elementBuffers,
                    IndexBufferViews = indexBufferViews,
                    PrimitiveTopologies = primitiveTopologies,
                    ElementsTypes = elementsTypes
                };
            }

            public D3D12Content.MaterialsCache MaterialsCache()
            {
                return new()
                {
                    RootSignatures = rootSignatures,
                    MaterialTypes = materialTypes
                };
            }

            public readonly uint Size()
            {
                return (uint)d3d12RenderItemIds.Length;
            }

            public void Clear()
            {
                Array.Resize(ref d3d12RenderItemIds, 0);
            }

            public void Resize()
            {
                ulong itemsCount = (ulong)d3d12RenderItemIds.Length;
                ulong newBufferSize = itemsCount * (ulong)structSize;
                ulong oldBufferSize = (ulong)buffer.Length;
                if (newBufferSize > oldBufferSize)
                {
                    Array.Resize(ref buffer, (int)newBufferSize);
                }

                if (newBufferSize != oldBufferSize)
                {
                    entityIds = new uint[itemsCount];
                    submeshGpuIds = new uint[itemsCount];
                    materialIds = new uint[itemsCount];
                    gpassPipelineStates = new ID3D12PipelineState[itemsCount];
                    depthPipelineStates = new ID3D12PipelineState[itemsCount];
                    rootSignatures = new ID3D12RootSignature[itemsCount];
                    materialTypes = new MaterialTypes[itemsCount];
                    positionBuffers = new ulong[itemsCount];
                    elementBuffers = new ulong[itemsCount];
                    indexBufferViews = new IndexBufferView[itemsCount];
                    primitiveTopologies = new D3D12PrimitiveTopology[itemsCount];
                    elementsTypes = new uint[itemsCount];
                    perObjectData = new ulong[itemsCount];
                }
            }

            static readonly int structSize =
                sizeof(uint) +                                  // entity_ids
                sizeof(uint) +                                  // submesh_gpu_ids
                sizeof(uint) +                                  // material_ids
                Marshal.SizeOf<IntPtr>() +         // gpass_pipeline_states
                Marshal.SizeOf<IntPtr>() +         // depth_pipeline_states
                Marshal.SizeOf<IntPtr>() +         // root_signatures
                sizeof(MaterialTypes) +                         // material_types
                sizeof(ulong) +                                 // position_buffers
                sizeof(ulong) +                                 // element_buffers
                Marshal.SizeOf<IndexBufferView>() +             // index_buffer_views
                sizeof(D3D12PrimitiveTopology) +                // primitive_topologies
                sizeof(uint) +                                  // elements_types
                sizeof(ulong)                                   // per_object_data
                ;

            byte[] buffer = [];

            public readonly void SetItems(D3D12Content.ItemsCache itemsCache)
            {
                Array.Copy(itemsCache.EntityIds, entityIds, itemsCache.EntityIds.Length);
                Array.Copy(itemsCache.SubmeshGpuIds, submeshGpuIds, itemsCache.SubmeshGpuIds.Length);
                Array.Copy(itemsCache.MaterialIds, materialIds, itemsCache.MaterialIds.Length);
                Array.Copy(itemsCache.GPassPsos, gpassPipelineStates, itemsCache.GPassPsos.Length);
                Array.Copy(itemsCache.DepthPsos, depthPipelineStates, itemsCache.DepthPsos.Length);
            }

            public readonly void SetViews(D3D12Content.ViewsCache viewsCache)
            {
                Array.Copy(viewsCache.PositionBuffers, positionBuffers, viewsCache.PositionBuffers.Length);
                Array.Copy(viewsCache.ElementBuffers, elementBuffers, viewsCache.ElementBuffers.Length);
                Array.Copy(viewsCache.IndexBufferViews, indexBufferViews, viewsCache.IndexBufferViews.Length);
                Array.Copy(viewsCache.PrimitiveTopologies, primitiveTopologies, viewsCache.PrimitiveTopologies.Length);
                Array.Copy(viewsCache.ElementsTypes, elementsTypes, viewsCache.ElementsTypes.Length);
            }

            public readonly void SetMaterials(D3D12Content.MaterialsCache materialsCache)
            {
                Array.Copy(materialsCache.RootSignatures, rootSignatures, materialsCache.RootSignatures.Length);
                Array.Copy(materialsCache.MaterialTypes, materialTypes, materialsCache.MaterialTypes.Length);
            }
        }

        public const Format mainBufferFormat = Format.R16G16B16A16_Float;
        public const Format depthBufferFormat = Format.D32_Float;
        static readonly SizeI initialDimensions = new() { Width = 100, Height = 100 };

        static D3D12RenderTexture gpassMainBuffer;
        static D3D12DepthBuffer gpassDepthBuffer;
        static SizeI dimensions = initialDimensions;

        static GPassCache frameCache = new();

#if DEBUG
        static readonly Color clearValue = new(0.5f, 0.5f, 0.5f, 1.0f);
#else
        static readonly Color clearValue = new(0.0f);
#endif

        public static D3D12RenderTexture MainBuffer { get => gpassMainBuffer; }
        public static D3D12DepthBuffer DepthBuffer { get => gpassDepthBuffer; }

        public static bool Initialize()
        {
            return CreateBuffers(initialDimensions);
        }
        private static bool CreateBuffers(SizeI size)
        {
            Debug.Assert(size.Width > 0 && size.Height > 0);
            gpassMainBuffer?.Dispose();
            gpassDepthBuffer?.Dispose();

            // Create the main buffer
            {
                ResourceDescription desc = new()
                {
                    Alignment = 0, // NOTE: 0 is the same as 64KB (or 4MB for MSAA)
                    DepthOrArraySize = 1,
                    Dimension = ResourceDimension.Texture2D,
                    Flags = ResourceFlags.AllowRenderTarget,
                    Format = mainBufferFormat,
                    Height = (uint)size.Height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = 0, // make space for all mip levels
                    SampleDescription = new(1, 0),
                    Width = (ulong)size.Width
                };

                D3D12TextureInitInfo info = new()
                {
                    Desc = desc,
                    InitialState = ResourceStates.PixelShaderResource,
                    ClearValue = new(desc.Format, clearValue),
                };

                gpassMainBuffer = new D3D12RenderTexture(info);
            }

            // Create the depth buffer
            {
                var desc = ResourceDescription.Texture2D(
                    depthBufferFormat,
                    (uint)size.Width,
                    (uint)size.Height,
                    1,
                    1,
                    flags: ResourceFlags.AllowDepthStencil);

                D3D12TextureInitInfo info = new()
                {
                    Desc = desc,
                    InitialState = ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
                    ClearValue = new(desc.Format, 1f, 0),
                };

                gpassDepthBuffer = new D3D12DepthBuffer(info);
            }

            D3D12Helpers.NameD3D12Object(gpassMainBuffer.Resource, "GPass Main Buffer");
            D3D12Helpers.NameD3D12Object(gpassDepthBuffer.Resource, "GPass Depth Buffer");

            return gpassMainBuffer.Resource != null && gpassDepthBuffer.Resource != null;
        }

        private static void FillPerObjectData(ConstantBuffer cbuffer, D3D12FrameInfo d3d12Info)
        {
            uint renderItemsCount = frameCache.Size();
            uint currentEntityId = uint.MaxValue;
            ulong currentGpuAddress = 0;

            for (uint i = 0; i < renderItemsCount; i++)
            {
                if (currentEntityId != frameCache.entityIds[i])
                {
                    currentEntityId = frameCache.entityIds[i];
                    PerObjectData data = new();
                    Transform.GetTransformMatrices(currentEntityId, out data.World, out data.InvWorld);
                    var world = data.World;
                    var wvp = Matrix4x4.Multiply(world, d3d12Info.Camera.ViewProjection);
                    data.WorldViewProjection = wvp;

                    currentGpuAddress = cbuffer.Write(data);
                }

                Debug.Assert(currentGpuAddress != 0);
                frameCache.perObjectData[i] = currentGpuAddress;
            }
        }
        private static void SetRootParameters(ID3D12GraphicsCommandList cmdList, uint cacheIndex)
        {
            Debug.Assert(cacheIndex < frameCache.Size());

            MaterialTypes mtlType = frameCache.materialTypes[cacheIndex];
            switch (mtlType)
            {
                case MaterialTypes.Opaque:
                {
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.PositionBuffer, frameCache.positionBuffers[cacheIndex]);
                    cmdList.SetGraphicsRootShaderResourceView((uint)OpaqueRootParameter.ElementBuffer, frameCache.elementBuffers[cacheIndex]);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.PerObjectData, frameCache.perObjectData[cacheIndex]);
                }
                break;
            }
        }
        private static void PrepareRenderFrame(D3D12FrameInfo d3d12Info)
        {
            Debug.Assert(d3d12Info.Camera != null);
            Debug.Assert(d3d12Info.FrameInfo.RenderItemIds != null && d3d12Info.FrameInfo.RenderItemCount > 0);
            frameCache.Clear();

            D3D12Content.GetD3D12RenderItemIds(ref d3d12Info.FrameInfo, ref frameCache.d3d12RenderItemIds);
            frameCache.Resize();
            uint itemsCount = frameCache.Size();
            D3D12Content.ItemsCache itemsCache = frameCache.ItemsCache();
            D3D12Content.GetItems(frameCache.d3d12RenderItemIds, itemsCount, ref itemsCache);
            frameCache.SetItems(itemsCache);

            D3D12Content.ViewsCache viewsCache = frameCache.ViewsCache();
            D3D12Content.GetSubmeshViews(itemsCache.SubmeshGpuIds, ref viewsCache);
            frameCache.SetViews(viewsCache);

            D3D12Content.MaterialsCache materialsCache = frameCache.MaterialsCache();
            D3D12Content.GetMaterials(itemsCache.MaterialIds, itemsCount, ref materialsCache);
            frameCache.SetMaterials(materialsCache);
        }

        public static void Shutdown()
        {
            gpassMainBuffer.Dispose();
            gpassMainBuffer = null;
            gpassDepthBuffer.Dispose();
            gpassDepthBuffer = null;
            dimensions = initialDimensions;
        }

        public static void SetSize(SizeI size)
        {
            if (size.Width <= dimensions.Width && size.Height <= dimensions.Height)
            {
                return;
            }

            dimensions.Width = Math.Max(size.Width, dimensions.Width);
            dimensions.Height = Math.Max(size.Height, dimensions.Height);

            CreateBuffers(dimensions);
        }

        public static void DepthPrePass(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo d3d12Info)
        {
            PrepareRenderFrame(d3d12Info);

            var cbuffer = D3D12Graphics.CBuffer;
            FillPerObjectData(cbuffer, d3d12Info);

            uint itemsCount = frameCache.Size();

            ID3D12RootSignature currentRootSignature = null;
            ID3D12PipelineState currentPipelineState = null;

            for (uint i = 0; i < itemsCount; i++)
            {
                if (currentRootSignature != frameCache.rootSignatures[i])
                {
                    currentRootSignature = frameCache.rootSignatures[i];
                    cmdList.SetGraphicsRootSignature(currentRootSignature);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.GlobalShaderData, d3d12Info.GlobalShaderData);
                }

                if (currentPipelineState != frameCache.depthPipelineStates[i])
                {
                    currentPipelineState = frameCache.depthPipelineStates[i];
                    cmdList.SetPipelineState(currentPipelineState);
                }

                SetRootParameters(cmdList, i);

                IndexBufferView ibv = frameCache.indexBufferViews[i];
                uint indexCount = ibv.SizeInBytes >> (ibv.Format == Format.R16_UInt ? 1 : 2);

                cmdList.IASetIndexBuffer(ibv);
                cmdList.IASetPrimitiveTopology(frameCache.primitiveTopologies[i]);
                cmdList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
            }
        }

        public static void Render(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo d3d12Info)
        {
            uint itemsCount = frameCache.Size();

            ID3D12RootSignature currentRootSignature = null;
            ID3D12PipelineState currentPipelineState = null;

            for (uint i = 0; i < itemsCount; i++)
            {
                if (currentRootSignature != frameCache.rootSignatures[i])
                {
                    currentRootSignature = frameCache.rootSignatures[i];
                    cmdList.SetGraphicsRootSignature(currentRootSignature);
                    cmdList.SetGraphicsRootConstantBufferView((uint)OpaqueRootParameter.GlobalShaderData, d3d12Info.GlobalShaderData);
                }

                if (currentPipelineState != frameCache.gpassPipelineStates[i])
                {
                    currentPipelineState = frameCache.gpassPipelineStates[i];
                    cmdList.SetPipelineState(currentPipelineState);
                }

                SetRootParameters(cmdList, i);

                IndexBufferView ibv = frameCache.indexBufferViews[i];
                uint indexCount = ibv.SizeInBytes >> (ibv.Format == Format.R16_UInt ? 1 : 2);

                cmdList.IASetIndexBuffer(ibv);
                cmdList.IASetPrimitiveTopology(frameCache.primitiveTopologies[i]);
                cmdList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
            }
        }
        public static void AddTransitionsForDepthPrePass(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.Resource,
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.BeginOnly);

            barriers.AddTransitionBarrier(
                gpassDepthBuffer.Resource,
                ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
                ResourceStates.DepthWrite);
        }
        public static void AddTransitionsForGPass(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.Resource,
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.EndOnly);

            barriers.AddTransitionBarrier(
                gpassDepthBuffer.Resource,
                ResourceStates.DepthWrite,
                ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource);
        }
        public static void AddTransitionsForPostProcess(D3D12ResourceBarrier barriers)
        {
            barriers.AddTransitionBarrier(
                gpassMainBuffer.Resource,
                ResourceStates.RenderTarget,
                ResourceStates.PixelShaderResource);
        }
        public static void SetRenderTargetsForDepthPrePass(ID3D12GraphicsCommandList cmdList)
        {
            var dsv = gpassDepthBuffer.Dsv;
            cmdList.ClearDepthStencilView(dsv.Cpu, ClearFlags.Depth | ClearFlags.Stencil, 1f, 0, 0, null);
            cmdList.OMSetRenderTargets([], dsv.Cpu);
        }
        public static void SetRenderTargetsForGPass(ID3D12GraphicsCommandList cmdList)
        {
            var rtv = gpassMainBuffer.GetRtv(0);
            var dsv = gpassDepthBuffer.Dsv;

            cmdList.ClearRenderTargetView(rtv, clearValue, 0, null);
            cmdList.OMSetRenderTargets(rtv, dsv.Cpu);
        }
    }
}
