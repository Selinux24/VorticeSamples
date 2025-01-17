using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    [StructLayout(LayoutKind.Sequential)]
    struct D3D12PipelineStateSubobjectStream()
    {
        public PipelineStateSubObjectTypeRootSignature RootSignature;
        public PipelineStateSubObjectTypeVertexShader Vs = new([]);
        public PipelineStateSubObjectTypePixelShader Ps = new([]);
        public PipelineStateSubObjectTypeDomainShader Ds = new([]);
        public PipelineStateSubObjectTypeHullShader Hs = new([]);
        public PipelineStateSubObjectTypeGeometryShader Gs = new([]);
        public PipelineStateSubObjectTypeComputeShader Cs = new([]);
        public PipelineStateSubObjectTypeStreamOutput StreamOutput = new(new([]));
        public PipelineStateSubObjectTypeBlend Blend = D3D12Helpers.BlendStatesCollection.Disabled;
        public PipelineStateSubObjectTypeSampleMask SampleMask = uint.MaxValue;
        public PipelineStateSubObjectTypeRasterizer Rasterizer = D3D12Helpers.RasterizerStatesCollection.NoCull;
        public PipelineStateSubObjectTypeInputLayout InputLayout = new(new([]));
        public PipelineStateSubObjectTypeIndexBufferStripCutValue IbStripCutValue = new(IndexBufferStripCutValue.Disabled);
        public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
        public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
        public PipelineStateSubObjectTypeDepthStencilFormat DepthStencilFormat;
        public PipelineStateSubObjectTypeSampleDescription SampleDesc = new SampleDescription(1, 0);
        public PipelineStateSubObjectTypeNodeMask NodeMask = new(0);
        public PipelineStateSubObjectTypeCachedPipelineState CachedPso = new(new());
        public PipelineStateSubObjectType Flags = PipelineStateSubObjectType.Flags;
        public PipelineStateSubObjectTypeDepthStencil1 DepthStencil1 = D3D12Helpers.DepthStatesCollection.Disabled;
        public PipelineStateSubObjectTypeViewInstancing ViewInstancing = new(new());
        public PipelineStateSubObjectTypeAmplificationShader As = new([]);
        public PipelineStateSubObjectTypeMeshShader Ms = new([]);
    }
}
