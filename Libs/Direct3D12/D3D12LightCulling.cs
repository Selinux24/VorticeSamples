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

            Count
        }
        struct CullingParameters()
        {
            public D3D12Buffer Frustums;
            public Shaders.LightCullingDispatchParameters GridFrustumsDispatchParams = new();
            public uint FrustumCount = 0;
            public uint ViewWidth = 0;
            public uint ViewHeight = 0;
            public float CameraFov = 0f;
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

        private static ID3D12RootSignature lightCullingRootSignature = null;
        private static ID3D12PipelineState gridFrustumPso = null;
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

            lightCullingRootSignature = new D3D12RootSignatureDesc(parameters).Create();
            D3D12Helpers.NameD3D12Object(lightCullingRootSignature, "Light Culling Root Signature");

            return lightCullingRootSignature != null;
        }
        private static bool CreatePsos()
        {
            Debug.Assert(gridFrustumPso == null);
            PipelineStateStream pipelineState = new()
            {
                RootSignature = new PipelineStateSubObjectTypeRootSignature(lightCullingRootSignature),
                Cs = new(D3D12Shaders.GetEngineShader(Shaders.EngineShaders.GridFrustumsCs).Span),
            };

            gridFrustumPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
            D3D12Helpers.NameD3D12Object(gridFrustumPso, "Grid Frustums PSO");

            return gridFrustumPso != null;
        }

        public static void CullLights(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo d3d12Info, D3D12ResourceBarrier barriers)
        {
            uint id = d3d12Info.LightCullingId;
            Debug.Assert(IdDetail.IsValid(id));
            var culler = lightCullers[id].Cullers[d3d12Info.FrameIndex];

            if (d3d12Info.SurfaceWidth != culler.ViewWidth ||
                d3d12Info.SurfaceHeight != culler.ViewHeight ||
                !MathHelper.IsZero(d3d12Info.Camera.FieldOfView - culler.CameraFov))
            {
                ResizeAndCalculateGridFrustums(ref culler, cmdList, d3d12Info, barriers);

                lightCullers[id].Cullers[d3d12Info.FrameIndex] = culler;
            }

            //barriers.Apply(cmdList);
        }
        private static void ResizeAndCalculateGridFrustums(
            ref CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            D3D12FrameInfo d3d12Info,
            D3D12ResourceBarrier barriers)
        {
            culler.CameraFov = d3d12Info.Camera.FieldOfView;
            culler.ViewWidth = d3d12Info.SurfaceWidth;
            culler.ViewHeight = d3d12Info.SurfaceHeight;

            Resize(ref culler);
            CalculateGridFrustums(ref culler, cmdList, d3d12Info, barriers);
        }
        private static void CalculateGridFrustums(
            ref CullingParameters culler,
            ID3D12GraphicsCommandList cmdList,
            D3D12FrameInfo d3d12Info,
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
            culler.GridFrustumsDispatchParams.NumThreads = tileCount;
            culler.GridFrustumsDispatchParams.NumThreadGroups.X = MathHelper.AlignUp(tileCount.X, tileSize) / tileSize;
            culler.GridFrustumsDispatchParams.NumThreadGroups.Y = MathHelper.AlignUp(tileCount.Y, tileSize) / tileSize;

            ResizeBuffers(ref culler);
        }
        private static void ResizeBuffers(ref CullingParameters culler)
        {
            uint frustumCount = culler.FrustumCount;
            uint frustumSize = (uint)Marshal.SizeOf<Shaders.Frustum>();
            uint frustumBufferSize = frustumSize * frustumCount;

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
        }

        public static void Shutdown()
        {
            D3D12Light.Shutdown();
            Debug.Assert(lightCullingRootSignature != null && gridFrustumPso != null);
            D3D12Graphics.DeferredRelease(lightCullingRootSignature);
            D3D12Graphics.DeferredRelease(gridFrustumPso);
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
    }
}
