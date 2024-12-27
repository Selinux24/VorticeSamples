global using D3D12Device = Vortice.Direct3D12.ID3D12Device8;
global using D3D12GraphicsCommandList = Vortice.Direct3D12.ID3D12GraphicsCommandList6;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    using HResult = SharpGen.Runtime.Result;

    static class D3D12Helpers
    {
        #region Structured Collections

        public readonly struct HeapPropertiesCollection()
        {
            private static readonly HeapProperties defaultHeap = new(
                HeapType.Default,
                CpuPageProperty.Unknown,
                MemoryPool.Unknown,
                0,
                0);

            public static HeapProperties DefaultHeap { get => defaultHeap; }
        }
        public readonly struct RasterizerStatesCollection()
        {
            private static readonly RasterizerDescription noCull = new(
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

            private static readonly RasterizerDescription backFaceCull = new(
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

            private static readonly RasterizerDescription frontFaceCull = new(
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

            private static readonly RasterizerDescription wireframe = new(
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

            public static RasterizerDescription NoCull { get => noCull; }
            public static RasterizerDescription BackFaceCull { get => backFaceCull; }
            public static RasterizerDescription FrontFaceCull { get => frontFaceCull; }
            public static RasterizerDescription Wireframe { get => wireframe; }
        }
        public readonly struct DepthStatesCollection()
        {
            private static readonly DepthStencilDescription1 disabled = new(
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

            public static DepthStencilDescription1 Disabled { get => disabled; }
        }

        #endregion

        public static bool DxCall(HResult result)
        {
            if (result.Success)
            {
                return true;
            }

            Debug.WriteLine($"DirectX call failed: {result.Description}");

            var removedReason = D3D12Graphics.Device.DeviceRemovedReason;
            if (!removedReason.Success)
            {
                Debug.WriteLine($"Device removed: {removedReason.Description}");
            }

#if DEBUG
            Debugger.Break();
#endif

            return false;
        }

        public static void NameD3D12Object(ID3D12Object obj, string name)
        {
            obj.Name = name;
            Debug.WriteLine($"D3D12 Object Created: {name}");
        }
        public static void NameD3D12Object(ID3D12Object obj, int index, string name)
        {
            string fullName = $"{name}[{index}]";
            obj.Name = fullName;
            Debug.WriteLine($"D3D12 Object Created: {fullName}");
        }

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

        public static ID3D12RootSignature CreateRootSignature(D3D12Device device, RootSignatureDescription1 desc)
        {
            VersionedRootSignatureDescription versionedDesc = new(desc);

            string error_msg = D3D12.D3D12SerializeVersionedRootSignature(versionedDesc, out var signature_blob);
            if (!string.IsNullOrEmpty(error_msg))
            {
                Debug.WriteLine(error_msg);
                return null;
            }

            if (!DxCall(device.CreateRootSignature<ID3D12RootSignature>(0, signature_blob.BufferPointer, signature_blob.BufferSize, out var signature)))
            {
                signature.Dispose();
            }

            return signature;
        }
    }
}