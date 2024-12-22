global using D3D12Device = Vortice.Direct3D12.ID3D12Device8;
global using D3D12GraphicsCommandList = Vortice.Direct3D12.ID3D12GraphicsCommandList6;
using System;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    static class D3D12Helpers
    {
        public readonly struct HeapPropertiesCollection()
        {
            public readonly HeapProperties DefaultHeap = new(
                HeapType.Default,
                CpuPageProperty.Unknown,
                MemoryPool.Unknown,
                0,
                0);
        }

        public readonly struct RasterizerStatesCollection()
        {
            public readonly RasterizerDescription NoCull = new(
                CullMode.None,
                FillMode.Solid,
                false,
                0,
                0,
                0,
                true,
                true,
                false,
                0,
                ConservativeRasterizationMode.Off);

            public readonly RasterizerDescription BackFaceCull = new(
                CullMode.Back,
                FillMode.Solid,
                false,
                0,
                0,
                0,
                true,
                true,
                false,
                0,
                ConservativeRasterizationMode.Off);

            public readonly RasterizerDescription FrontFaceCull = new(
                CullMode.Front,
                FillMode.Solid,
                false,
                0,
                0,
                0,
                true,
                true,
                false,
                0,
                ConservativeRasterizationMode.Off);

            public readonly RasterizerDescription Wireframe = new(
                CullMode.None,
                FillMode.Wireframe,
                false,
                0,
                0,
                0,
                true,
                true,
                false,
                0,
                ConservativeRasterizationMode.Off);
        }

        public readonly struct DephStatesCollection()
        {
            public readonly DepthStencilDescription1 Disabled = new(
                false,
                false,
                ComparisonFunction.LessEqual,
                false,
                0,
                0,
                StencilOperation.Zero,
                StencilOperation.Zero,
                StencilOperation.Zero,
                ComparisonFunction.None,
                StencilOperation.Zero,
                StencilOperation.Zero,
                StencilOperation.Zero,
                ComparisonFunction.None,
                false);
        }

        public static readonly HeapPropertiesCollection HeapProperties = new();
        public static readonly RasterizerStatesCollection RasterizerState = new();
        public static readonly DephStatesCollection DepthState = new();

        public static DescriptorRange1 Range(
            DescriptorRangeType rangeType,
            int descriptorCount,
            int shaderRegister,
            int space = 0,
            DescriptorRangeFlags flags = DescriptorRangeFlags.None,
            int offsetFromTableStart = D3D12.DescriptorRangeOffsetAppend)
        {
            return new()
            {
                RangeType = rangeType,
                NumDescriptors = descriptorCount,
                BaseShaderRegister = shaderRegister,
                RegisterSpace = space,
                Flags = flags,
                OffsetInDescriptorsFromTableStart = offsetFromTableStart,
            };
        }

        public static RootParameter1 AsConstants(
            int numConstants,
            ShaderVisibility visibility,
            int shaderRegister,
            int space = 0)
        {
            RootConstants rootConstants = new(shaderRegister, space, numConstants);

            return new(rootConstants, visibility);
        }

        private static RootParameter1 AsDescriptor(
            RootParameterType type,
            ShaderVisibility visibility,
            int shaderRegister,
            int space,
            RootDescriptorFlags flags)
        {
            RootDescriptor1 rootDescriptor = new(shaderRegister, space, flags);

            return new(type, rootDescriptor, visibility);
        }

        public static RootParameter1 AsCbv(
            ShaderVisibility visibility,
            int shaderRegister,
            int space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.ConstantBufferView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsSrv(
            ShaderVisibility visibility,
            int shaderRegister,
            int space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.ShaderResourceView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsUav(
            ShaderVisibility visibility,
            int shaderRegister,
            int space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.UnorderedAccessView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsDescriptorTable(
            ShaderVisibility visibility,
            DescriptorRange1[] ranges)
        {
            RootDescriptorTable1 table = new(ranges);

            return new(table, visibility);
        }

        // Maximum 64 DWORDs (u32's) divided up amongst all root parameters.
        // Root constants = 1 DWORD per 32-bit constant
        // Root descriptor  (CBV, SRV or UAV) = 2 DWORDs each
        // Descriptor table pointer = 1 DWORD
        // Static samplers = 0 DWORDs (compiled into shader)
        public static RootSignatureDescription1 AsRootSignatureDesc(
            RootParameter1[] parameters,
            StaticSamplerDescription[] staticSamplers = null,
            RootSignatureFlags flags =
                RootSignatureFlags.DenyVertexShaderRootAccess |
                RootSignatureFlags.DenyHullShaderRootAccess |
                RootSignatureFlags.DenyDomainShaderRootAccess |
                RootSignatureFlags.DenyGeometryShaderRootAccess |
                RootSignatureFlags.DenyAmplificationShaderRootAccess |
                RootSignatureFlags.DenyMeshShaderRootAccess)
        {
            return new RootSignatureDescription1(flags, parameters, staticSamplers);
        }

        public static void TransitionResource(
            ID3D12GraphicsCommandList cmdList,
            ID3D12Resource resource,
            ResourceStates before,
            ResourceStates after,
            ResourceBarrierFlags flags = ResourceBarrierFlags.None,
            int subresource = D3D12.ResourceBarrierAllSubResources)
        {
            cmdList.ResourceBarrierTransition(resource, before, after, subresource, flags);
        }

        public static ID3D12RootSignature CreateRootSignature(D3D12Device device, RootSignatureDescription1 desc)
        {
            VersionedRootSignatureDescription versionedDesc = new(desc);

            string error_msg = D3D12.D3D12SerializeVersionedRootSignature(versionedDesc, out var signature_blob);
            if (!string.IsNullOrEmpty(error_msg))
            {
                Debug.WriteLine(error_msg);
                return null;
            }

            if (!device.CreateRootSignature<ID3D12RootSignature>(0, signature_blob.BufferPointer, signature_blob.BufferSize, out var signature).Success)
            {
                signature.Release();
            }

            return signature;
        }

        public static ID3D12PipelineState CreatePipelineState(D3D12Device device, PipelineStateStreamDescription desc)
        {
            Debug.Assert(desc.SubObjectStream != 0 && desc.SizeInBytes != 0);
            if (!device.CreatePipelineState<ID3D12PipelineState>(desc, out var pso).Success)
            {
                Debug.WriteLine("Error creating the pipeline state.");
            }

            Debug.Assert(pso != null);
            return pso;
        }

        public static ID3D12PipelineState CreatePipelineState(D3D12Device device, IntPtr stream, int stream_size)
        {
            Debug.Assert(stream != 0 && stream_size != 0);
            PipelineStateStreamDescription desc = new()
            {
                SizeInBytes = stream_size,
                SubObjectStream = stream
            };
            return CreatePipelineState(device, desc);
        }
    }
}