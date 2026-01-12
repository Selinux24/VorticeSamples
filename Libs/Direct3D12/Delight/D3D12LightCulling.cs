using Direct3D12.Lights;
using PrimalLike.Common;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12.Delight
{
    static class D3D12LightCulling
    {
        #region Structures & enumerations

        enum LightCullingRootParameters : uint
        {
            GlobalShaderData,
            Constants,
            FrustumsOutOrIndexCounter,
            FrustumsIn,
            CullingInfo,
            BoundingSpheres,
            LightGridOpaque,
            LightIndexListOpaque,

            Count
        }
        struct CullingParameters() : IDisposable
        {
            public D3D12Buffer Frustums;
            public D3D12Buffer LightGridAndIndexList;
            public UavClearableBuffer LightIndexCounter;
            public Shaders.LightCullingDispatchParameters GridFrustumsDispatchParams = new();
            public Shaders.LightCullingDispatchParameters LightCullingDispatchParams = new();
            public uint FrustumCount = 0;
            public uint ViewWidth = 0;
            public uint ViewHeight = 0;
            public float CameraFov = 0f;
            public ulong LightIndexListOpaqueBuffer = 0;
            // NOTE: initialize has_lights with 'true' so that the culling shader
            //       is run at least once in order to clear the buffer.
            public bool HasLights = true;

            public void Dispose()
            {
                Frustums?.Dispose();
                LightIndexCounter?.Dispose();
                LightGridAndIndexList?.Dispose();
                LightIndexCounter = null;
                LightGridAndIndexList = null;
            }
        }
        struct LightCuller : IDisposable
        {
            public CullingParameters[] Cullers;

            public LightCuller()
            {
                Cullers = new CullingParameters[D3D12Graphics.FrameBufferCount];
                for (int i = 0; i < D3D12Graphics.FrameBufferCount; i++)
                {
                    Cullers[i] = new();
                }
            }
            public void Dispose()
            {
                foreach (var culler in Cullers)
                {
                    culler.Dispose();
                }
                Cullers = null;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        struct PipelineStateStream
        {
            public PipelineStateSubObjectTypeRootSignature RootSignature;
            public PipelineStateSubObjectTypeComputeShader Cs;
        }

        #endregion

        const uint LightCullingTileSize = 32;
        const uint MaxLightsPerTile = 256;

        static ID3D12RootSignature lightCullingRootSignature = null;
        static ID3D12PipelineState gridFrustumPso = null;
        static ID3D12PipelineState lightCullingPso = null;
        static readonly FreeList<LightCuller> lightCullers = new();

        public static bool Initialize()
        {
            return
                CreateRootSignatures() &&
                CreatePsos() &&
                D3D12Light.Initialize();
        }
        /// <summary>
        /// Creates the root signatures for light culling.
        /// </summary>
        /// <remarks>
        /// See CullLightsCS in CullLights.hlsl for the root signature layout.
        /// </remarks>
        static bool CreateRootSignatures()
        {
            Debug.Assert(lightCullingRootSignature == null);
            RootParameter1[] parameters = new RootParameter1[(uint)LightCullingRootParameters.Count];

            parameters[(uint)LightCullingRootParameters.GlobalShaderData] = D3D12Helpers.AsCbv(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.Constants] = D3D12Helpers.AsCbv(ShaderVisibility.All, 1);

            parameters[(uint)LightCullingRootParameters.FrustumsIn] = D3D12Helpers.AsSrv(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.CullingInfo] = D3D12Helpers.AsSrv(ShaderVisibility.All, 1);
            parameters[(uint)LightCullingRootParameters.BoundingSpheres] = D3D12Helpers.AsSrv(ShaderVisibility.All, 2);

            parameters[(uint)LightCullingRootParameters.FrustumsOutOrIndexCounter] = D3D12Helpers.AsUav(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.LightGridOpaque] = D3D12Helpers.AsUav(ShaderVisibility.All, 1);
            parameters[(uint)LightCullingRootParameters.LightIndexListOpaque] = D3D12Helpers.AsUav(ShaderVisibility.All, 3);

            lightCullingRootSignature = new D3D12RootSignatureDesc(parameters).Create();
            D3D12Helpers.NameD3D12Object(lightCullingRootSignature, "Light Culling Root Signature");

            return lightCullingRootSignature != null;
        }
        static bool CreatePsos()
        {
            {
                Debug.Assert(gridFrustumPso == null);
                PipelineStateStream pipelineState = new()
                {
                    RootSignature = new PipelineStateSubObjectTypeRootSignature(lightCullingRootSignature),
                    Cs = new(D3D12Shaders.GetEngineShader(EngineShaders.GridFrustumsCs).Span),
                };

                gridFrustumPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
                D3D12Helpers.NameD3D12Object(gridFrustumPso, "Grid Frustums PSO");
            }
            {
                Debug.Assert(lightCullingPso == null);
                PipelineStateStream pipelineState = new()
                {
                    RootSignature = new PipelineStateSubObjectTypeRootSignature(lightCullingRootSignature),
                    Cs = new(D3D12Shaders.GetEngineShader(EngineShaders.LightCullingCs).Span),
                };

                lightCullingPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
                D3D12Helpers.NameD3D12Object(lightCullingPso, "Light Culling PSO");
            }

            return gridFrustumPso != null && lightCullingPso != null;
        }

        public static void Shutdown()
        {
            D3D12Light.Shutdown();

            Debug.Assert(lightCullingRootSignature != null);
            D3D12Graphics.DeferredRelease(lightCullingRootSignature);

            Debug.Assert(gridFrustumPso != null);
            D3D12Graphics.DeferredRelease(gridFrustumPso);

            Debug.Assert(lightCullingPso != null);
            D3D12Graphics.DeferredRelease(lightCullingPso);
        }

        public static void CullLights(ID3D12GraphicsCommandList cmdList, ref D3D12FrameInfo d3d12Info, D3D12ResourceBarrier barriers)
        {
            uint id = d3d12Info.LightCullingId;
            Debug.Assert(IdDetail.IsValid(id));
            var culler = lightCullers[id].Cullers[d3d12Info.FrameIndex];

            if (d3d12Info.SurfaceWidth != culler.ViewWidth ||
                d3d12Info.SurfaceHeight != culler.ViewHeight ||
                !MathHelper.IsZero(d3d12Info.Camera.FieldOfView - culler.CameraFov))
            {
                ResizeAndCalculateGridFrustums(ref culler, cmdList, ref d3d12Info, barriers);

                lightCullers[id].Cullers[d3d12Info.FrameIndex] = culler;
            }

            culler.LightCullingDispatchParams.NumLights = D3D12Light.CullableLightCount(d3d12Info.FrameInfo.LightSetKey);
            culler.LightCullingDispatchParams.DepthBufferSrvIndex = D3D12GPass.DepthBuffer.GetSrv().Index;

            // NOTE: we update culler.has_lights after this statement, so the light culling shader
            //       will run once to clear the buffers when there're no lights.
            if (culler.LightCullingDispatchParams.NumLights == 0 && !culler.HasLights)
            {
                return;
            }

            culler.HasLights = culler.LightCullingDispatchParams.NumLights > 0;

            ulong gpuAddress = D3D12Graphics.CBuffer.Write(culler.LightCullingDispatchParams);

            // Make light grid and light index buffers writable
            barriers.AddTransitionBarrier(
                culler.LightGridAndIndexList.Buffer,
                ResourceStates.PixelShaderResource,
                ResourceStates.UnorderedAccess);
            barriers.Apply(cmdList);

            culler.LightIndexCounter.ClearUav(cmdList, Int4.Zero);

            cmdList.SetComputeRootSignature(lightCullingRootSignature);
            cmdList.SetPipelineState(lightCullingPso);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.GlobalShaderData, d3d12Info.GlobalShaderData);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.Constants, gpuAddress);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.FrustumsOutOrIndexCounter, culler.LightIndexCounter.GpuAddress);
            cmdList.SetComputeRootShaderResourceView((uint)LightCullingRootParameters.FrustumsIn, culler.Frustums.GpuAddress);
            cmdList.SetComputeRootShaderResourceView((uint)LightCullingRootParameters.CullingInfo, D3D12Light.CullingInfoBuffer(d3d12Info.FrameIndex));
            cmdList.SetComputeRootShaderResourceView((uint)LightCullingRootParameters.BoundingSpheres, D3D12Light.BoundingSpheresBuffer(d3d12Info.FrameIndex));
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.LightGridOpaque, culler.LightGridAndIndexList.GpuAddress);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.LightIndexListOpaque, culler.LightIndexListOpaqueBuffer);

            cmdList.Dispatch(culler.LightCullingDispatchParams.NumThreadGroups.X, culler.LightCullingDispatchParams.NumThreadGroups.Y, 1);

            // Make light grid and light index buffers readable
            // NOTE: this transition barrier will be applied by the caller of this function.
            barriers.AddTransitionBarrier(
                culler.LightGridAndIndexList.Buffer,
                ResourceStates.UnorderedAccess,
                ResourceStates.PixelShaderResource);
        }
        static void ResizeAndCalculateGridFrustums(
            ref CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            ref D3D12FrameInfo d3d12Info,
            D3D12ResourceBarrier barriers)
        {
            culler.CameraFov = d3d12Info.Camera.FieldOfView;
            culler.ViewWidth = d3d12Info.SurfaceWidth;
            culler.ViewHeight = d3d12Info.SurfaceHeight;

            Resize(ref culler);
            CalculateGridFrustums(culler, cmdList, ref d3d12Info, barriers);
        }
        static void CalculateGridFrustums(
            CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            ref D3D12FrameInfo d3d12Info,
            D3D12ResourceBarrier barriers)
        {
            ulong gridFrustumsDispatchParamsAddress = D3D12Graphics.CBuffer.Write(culler.GridFrustumsDispatchParams);

            // Make frustums buffer writable
            // TODO: remove pixel_shader_resource flag (it's only there so we can visualize grid frustums).
            barriers.AddTransitionBarrier(
                culler.Frustums.Buffer,
                ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource,
                ResourceStates.UnorderedAccess);
            barriers.Apply(cmdList);

            cmdList.SetComputeRootSignature(lightCullingRootSignature);
            cmdList.SetPipelineState(gridFrustumPso);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.GlobalShaderData, d3d12Info.GlobalShaderData);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.Constants, gridFrustumsDispatchParamsAddress);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.FrustumsOutOrIndexCounter, culler.Frustums.GpuAddress);
            cmdList.Dispatch(culler.GridFrustumsDispatchParams.NumThreadGroups.X, culler.GridFrustumsDispatchParams.NumThreadGroups.Y, 1);

            // Make frustums buffer readable
            // NOTE: cull_lights() will apply this transition.
            // TODO: remove pixel_shader_resource flag (it's only there so we can visualize grid frustums).
            barriers.AddTransitionBarrier(
                culler.Frustums.Buffer,
                ResourceStates.UnorderedAccess,
                ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource);
        }
        static void Resize(ref CullingParameters culler)
        {
            uint tileSize = LightCullingTileSize;
            Debug.Assert(culler.ViewWidth >= tileSize && culler.ViewHeight >= tileSize);
            UInt2 tileCount = new(
                MathHelper.AlignUp(culler.ViewWidth, tileSize) / tileSize,
                MathHelper.AlignUp(culler.ViewHeight, tileSize) / tileSize);

            culler.FrustumCount = tileCount.X * tileCount.Y;

            // Dispatch parameters for grid frustums
            {
                culler.GridFrustumsDispatchParams.NumThreads = tileCount;
                culler.GridFrustumsDispatchParams.NumThreadGroups.X = MathHelper.AlignUp(tileCount.X, tileSize) / tileSize;
                culler.GridFrustumsDispatchParams.NumThreadGroups.Y = MathHelper.AlignUp(tileCount.Y, tileSize) / tileSize;
            }

            // Dispatch parameters for light culling
            {
                culler.LightCullingDispatchParams.NumThreads.X = tileCount.X * tileSize;
                culler.LightCullingDispatchParams.NumThreads.Y = tileCount.Y * tileSize;
                culler.LightCullingDispatchParams.NumThreadGroups = tileCount;
            }

            ResizeBuffers(ref culler);
        }
        static void ResizeBuffers(ref CullingParameters culler)
        {
            uint alignment = (uint)Marshal.SizeOf<Vector4>();
            uint frustumsBufferStride = (uint)Marshal.SizeOf<Shaders.Frustum>();
            uint lightGridBufferStride = (uint)Marshal.SizeOf<UInt2>();
            uint lightIndexListBufferStride = sizeof(uint);

            uint frustumCount = culler.FrustumCount;
            uint frustumsBufferSize = frustumsBufferStride * frustumCount;
            uint lightGridBufferSize = MathHelper.AlignUp(lightGridBufferStride * frustumCount, alignment);
            uint lightIndexListBufferSize = MathHelper.AlignUp(lightIndexListBufferStride * frustumCount * MaxLightsPerTile, alignment);
            uint lightGridAndIndexListBufferSize = lightGridBufferSize + lightIndexListBufferSize;

            if (frustumsBufferSize > (culler.Frustums?.Size ?? 0))
            {
                D3D12BufferInitInfo info = new()
                {
                    Alignment = alignment,
                    Flags = ResourceFlags.AllowUnorderedAccess,
                    Size = frustumsBufferSize,
                };

                culler.Frustums = new D3D12Buffer(info, false);
                D3D12Helpers.NameD3D12Object(culler.Frustums.Buffer, frustumCount, "Light Grid Frustums Buffer - count");
            }

            if (lightGridAndIndexListBufferSize > (culler.LightGridAndIndexList?.Size ?? 0))
            {
                D3D12BufferInitInfo info = new()
                {
                    Alignment = alignment,
                    Flags = ResourceFlags.AllowUnorderedAccess,
                    Size = lightGridAndIndexListBufferSize,
                };

                culler.LightGridAndIndexList = new(info, false);

                culler.LightIndexListOpaqueBuffer = culler.LightGridAndIndexList.GpuAddress + lightGridBufferSize;
                D3D12Helpers.NameD3D12Object(culler.LightGridAndIndexList.Buffer, lightGridAndIndexListBufferSize, "Light Grid and Index List Buffer - size");

                if (culler.LightIndexCounter?.Buffer == null)
                {
                    info = UavClearableBuffer.GetDefaultInitInfo(1);
                    culler.LightIndexCounter = new(info);
                    D3D12Helpers.NameD3D12Object(culler.LightIndexCounter.Buffer, D3D12Graphics.CurrentFrameIndex, "Light Index Counter Buffer");
                }
            }
        }

        public static uint AddCuller()
        {
            return lightCullers.Add(new());
        }
        public static void RemoveCuller(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            lightCullers.Remove(id);
        }

        // TODO: temporary for visualizing light culling. Remove later.
        public static ulong Frustums(uint lightCullingId, uint frameIndex)
        {
            Debug.Assert(frameIndex < D3D12Graphics.FrameBufferCount && IdDetail.IsValid(lightCullingId));
            return lightCullers[lightCullingId].Cullers[frameIndex].Frustums.GpuAddress;
        }
        public static ulong LightGridOpaque(uint lightCullingId, uint frameIndex)
        {
            Debug.Assert(frameIndex < D3D12Graphics.FrameBufferCount && IdDetail.IsValid(lightCullingId));
            return lightCullers[lightCullingId].Cullers[frameIndex].LightGridAndIndexList.GpuAddress;
        }
        public static ulong LightIndexListOpaque(uint lightCullingId, uint frameIndex)
        {
            Debug.Assert(frameIndex < D3D12Graphics.FrameBufferCount && IdDetail.IsValid(lightCullingId));
            return lightCullers[lightCullingId].Cullers[frameIndex].LightIndexListOpaqueBuffer;
        }
    }
}
