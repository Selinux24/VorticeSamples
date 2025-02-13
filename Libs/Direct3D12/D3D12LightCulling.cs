using PrimalLike.Common;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12
{
    static class D3D12LightCulling
    {
        const uint LightCullingTileSize = 16;

        enum LightCullingRootParameters : uint
        {
            GlobalShaderData,
            Constants,
            FrustumsOutOrIndexCounter,
            FrustumsIn,
            CullingInfo,
            LightGridOpaque,
            LightIndexListOpaque,

            Count
        }
        struct CullingParameters()
        {
            public D3D12Buffer Frustums;
            public D3D12Buffer LightGridAndIndexList;
            public Shaders.D3D12StructuredBuffer LightIndexCounter;
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
        }
        struct LightCuller
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
        }
        [StructLayout(LayoutKind.Sequential)]
        struct PipelineStateStream
        {
            public PipelineStateSubObjectTypeRootSignature RootSignature;
            public PipelineStateSubObjectTypeComputeShader Cs;
        }

        const uint MaxLightsPerTile = 256;

        private static ID3D12RootSignature lightCullingRootSignature = null;
        private static ID3D12PipelineState gridFrustumPso = null;
        private static ID3D12PipelineState lightCullingPso = null;
        private static readonly FreeList<LightCuller> lightCullers = new();

        public static bool Initialize()
        {
            return CreateRootSignatures() && CreatePsos() && D3D12Light.Initialize();
        }
        private static bool CreateRootSignatures()
        {
            Debug.Assert(lightCullingRootSignature == null);
            RootParameter1[] parameters = new RootParameter1[(uint)LightCullingRootParameters.Count];
            parameters[(uint)LightCullingRootParameters.GlobalShaderData] = D3D12Helpers.AsCbv(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.Constants] = D3D12Helpers.AsCbv(ShaderVisibility.All, 1);
            parameters[(uint)LightCullingRootParameters.FrustumsOutOrIndexCounter] = D3D12Helpers.AsUav(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.FrustumsIn] = D3D12Helpers.AsSrv(ShaderVisibility.All, 0);
            parameters[(uint)LightCullingRootParameters.CullingInfo] = D3D12Helpers.AsSrv(ShaderVisibility.All, 1);
            parameters[(uint)LightCullingRootParameters.LightGridOpaque] = D3D12Helpers.AsUav(ShaderVisibility.All, 1);
            parameters[(uint)LightCullingRootParameters.LightIndexListOpaque] = D3D12Helpers.AsUav(ShaderVisibility.All, 3);

            lightCullingRootSignature = new D3D12RootSignatureDesc(parameters).Create();
            D3D12Helpers.NameD3D12Object(lightCullingRootSignature, "Light Culling Root Signature");

            return lightCullingRootSignature != null;
        }
        private static bool CreatePsos()
        {
            {
                Debug.Assert(gridFrustumPso == null);
                PipelineStateStream pipelineState = new()
                {
                    RootSignature = new PipelineStateSubObjectTypeRootSignature(lightCullingRootSignature),
                    Cs = new(D3D12Shaders.GetEngineShader(Shaders.EngineShaders.GridFrustumsCs).Span),
                };

                gridFrustumPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
                D3D12Helpers.NameD3D12Object(gridFrustumPso, "Grid Frustums PSO");
            }
            {
                Debug.Assert(lightCullingPso == null);
                PipelineStateStream pipelineState = new()
                {
                    RootSignature = new PipelineStateSubObjectTypeRootSignature(lightCullingRootSignature),
                    Cs = new(D3D12Shaders.GetEngineShader(Shaders.EngineShaders.LightCullingCs).Span),
                };

                lightCullingPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
                D3D12Helpers.NameD3D12Object(lightCullingPso, "Light Culling PSO");
            }

            return gridFrustumPso != null && lightCullingPso != null;
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

            var cbuffer = D3D12Graphics.CBuffer;
            ulong buffer = cbuffer.Write(culler.LightCullingDispatchParams);

            // Make light grid and light index buffers writable
            barriers.AddTransitionBarrier(
                culler.LightGridAndIndexList.Buffer,
                ResourceStates.PixelShaderResource, ResourceStates.UnorderedAccess);
            barriers.Apply(cmdList);

            Int4 clearValue = new(0, 0, 0, 0);
            culler.LightIndexCounter.ClearUav(cmdList, clearValue);

            cmdList.SetComputeRootSignature(lightCullingRootSignature);
            cmdList.SetPipelineState(lightCullingPso);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.GlobalShaderData, d3d12Info.GlobalShaderData);
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.Constants, buffer);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.FrustumsOutOrIndexCounter, culler.LightIndexCounter.GpuAddress);
            cmdList.SetComputeRootShaderResourceView((uint)LightCullingRootParameters.FrustumsIn, culler.Frustums.GpuAddress);
            cmdList.SetComputeRootShaderResourceView((uint)LightCullingRootParameters.CullingInfo, D3D12Light.CullingInfoBuffer(d3d12Info.FrameIndex));
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.LightGridOpaque, culler.LightGridAndIndexList.GpuAddress);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.LightIndexListOpaque, culler.LightIndexListOpaqueBuffer);

            cmdList.Dispatch(culler.LightCullingDispatchParams.NumThreadGroups.X, culler.LightCullingDispatchParams.NumThreadGroups.Y, 1);

            // Make light grid and light index buffers readable
            // NOTE: this transition barrier will be applied by the caller of this function.
            barriers.AddTransitionBarrier(
                culler.LightGridAndIndexList.Buffer,
                ResourceStates.UnorderedAccess, ResourceStates.PixelShaderResource);
        }
        private static void ResizeAndCalculateGridFrustums(
            ref CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            ref D3D12FrameInfo d3d12Info,
            D3D12ResourceBarrier barriers)
        {
            culler.CameraFov = d3d12Info.Camera.FieldOfView;
            culler.ViewWidth = d3d12Info.SurfaceWidth;
            culler.ViewHeight = d3d12Info.SurfaceHeight;

            Resize(ref culler);
            CalculateGridFrustums(ref culler, cmdList, ref d3d12Info, barriers);
        }
        private static void CalculateGridFrustums(
            ref CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            ref D3D12FrameInfo d3d12Info,
            D3D12ResourceBarrier barriers)
        {
            var cbuffer = D3D12Graphics.CBuffer;
            var parameters = culler.GridFrustumsDispatchParams;
            ulong bufferAddress = cbuffer.Write(parameters);

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
            cmdList.SetComputeRootConstantBufferView((uint)LightCullingRootParameters.Constants, bufferAddress);
            cmdList.SetComputeRootUnorderedAccessView((uint)LightCullingRootParameters.FrustumsOutOrIndexCounter, culler.Frustums.GpuAddress);
            cmdList.Dispatch(parameters.NumThreadGroups.X, parameters.NumThreadGroups.Y, 1);

            // Make frustums buffer readable
            // NOTE: cull_lights() will apply this transition.
            // TODO: remove pixel_shader_resource flag (it's only there so we can visualize grid frustums).
            barriers.AddTransitionBarrier(
                culler.Frustums.Buffer,
                ResourceStates.UnorderedAccess,
                ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource);
        }
        private static void Resize(ref CullingParameters culler)
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
        private static void ResizeBuffers(ref CullingParameters culler)
        {
            uint frustumCount = culler.FrustumCount;
            uint frustumSize = (uint)Marshal.SizeOf<Shaders.Frustum>();
            uint frustumBufferSize = frustumSize * frustumCount;
            uint lightGridBufferSize = MathHelper.AlignUp((uint)Marshal.SizeOf<UInt2>() * frustumCount, (uint)Marshal.SizeOf<Vector4>());
            uint lightIndexBufferSize = MathHelper.AlignUp(sizeof(uint) * MaxLightsPerTile * frustumCount, (uint)Marshal.SizeOf<Vector4>());
            uint lightGridAndIndexListBufferSize = lightGridBufferSize + lightIndexBufferSize;

            if (frustumBufferSize > (culler.Frustums?.Size ?? 0))
            {
                D3D12BufferInitInfo info = new()
                {
                    Alignment = (uint)Marshal.SizeOf<Vector4>(),
                    Flags = ResourceFlags.AllowUnorderedAccess,
                    Size = frustumBufferSize,
                };

                culler.Frustums = new D3D12Buffer(info, false);
                D3D12Helpers.NameD3D12Object(culler.Frustums.Buffer, frustumCount, "Light Grid Frustums Buffer - count");
            }

            if (lightGridAndIndexListBufferSize > (culler.LightGridAndIndexList?.Size ?? 0))
            {
                D3D12BufferInitInfo info = new()
                {
                    Alignment = (uint)Marshal.SizeOf<Vector4>(),
                    Flags = ResourceFlags.AllowUnorderedAccess,
                    Size = lightGridAndIndexListBufferSize,
                };

                culler.LightGridAndIndexList = new(info, false);

                ulong lightGridOpaqueBuffer = culler.LightGridAndIndexList.GpuAddress;
                culler.LightIndexListOpaqueBuffer = lightGridOpaqueBuffer + lightGridBufferSize;
                D3D12Helpers.NameD3D12Object(culler.LightGridAndIndexList.Buffer, lightGridAndIndexListBufferSize, "Light Grid and Index List Buffer - size");

                if (culler.LightIndexCounter?.Buffer == null)
                {
                    info = Shaders.D3D12StructuredBuffer.GetDefaultInitInfo((uint)Marshal.SizeOf<Int4>(), 1);
                    info.CreateUav = true;
                    culler.LightIndexCounter = new(info);
                    D3D12Helpers.NameD3D12Object(culler.LightIndexCounter.Buffer, D3D12Graphics.CurrentFrameIndex, "Light Index Counter Buffer");
                }
            }
        }

        public static void Shutdown()
        {
            D3D12Light.Shutdown();
            Debug.Assert(lightCullingRootSignature != null && gridFrustumPso != null && lightCullingPso != null);
            D3D12Graphics.DeferredRelease(lightCullingRootSignature);
            D3D12Graphics.DeferredRelease(gridFrustumPso);
            D3D12Graphics.DeferredRelease(lightCullingPso);
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
