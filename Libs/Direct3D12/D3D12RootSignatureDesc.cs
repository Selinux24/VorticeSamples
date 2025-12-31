using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// Helper struct to create a D3D12 root signature.
    /// </summary>
    /// <param name="parameters">Root parameters</param>
    /// <param name="flags">Root signature flags</param>
    /// <param name="staticSamplers">Static samplers</param>
    /// <remarks>
    /// Maximum 64 DWORDs (u32's) divided up amongst all root parameters.
    /// Root constants = 1 DWORD per 32-bit constant
    /// Root descriptor  (CBV, SRV or UAV) = 2 DWORDs each
    /// Descriptor table pointer = 1 DWORD
    /// Static samplers = 0 DWORDs (compiled into shader)
    /// </remarks>
    struct D3D12RootSignatureDesc(
        RootParameter1[] parameters,
        RootSignatureFlags flags = D3D12RootSignatureDesc.DefaultFlags,
        StaticSamplerDescription[] staticSamplers = null)
    {
        public const RootSignatureFlags DefaultFlags =
            RootSignatureFlags.DenyVertexShaderRootAccess |
            RootSignatureFlags.DenyHullShaderRootAccess |
            RootSignatureFlags.DenyDomainShaderRootAccess |
            RootSignatureFlags.DenyGeometryShaderRootAccess |
            RootSignatureFlags.DenyPixelShaderRootAccess |
            RootSignatureFlags.DenyAmplificationShaderRootAccess |
            RootSignatureFlags.DenyMeshShaderRootAccess |
            RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed;

        readonly RootSignatureDescription1 desc = new(flags, parameters, staticSamplers);

        public readonly RootParameter1[] Parameters
        {
            get => desc.Parameters;
            set => desc.Parameters = value;
        }
        public readonly StaticSamplerDescription[] StaticSamplers
        {
            get => desc.StaticSamplers;
            set => desc.StaticSamplers = value;
        }
        public readonly RootSignatureFlags Flags
        {
            get => desc.Flags;
            set => desc.Flags = value;
        }

        public readonly ID3D12RootSignature Create()
        {
            return D3D12Helpers.CreateRootSignature(desc);
        }
    }
}
