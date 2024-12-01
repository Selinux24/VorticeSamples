using Vortice.Direct3D12;

namespace Direct3D12
{
    struct RenderTargetData
    {
        public ID3D12Resource Resource;
        public DescriptorHandle Rtv;
    };
}
