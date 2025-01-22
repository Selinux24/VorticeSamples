using Vortice.Direct3D12;

namespace Direct3D12.Content
{
    struct ItemsCache
    {
        public uint[] EntityIds;
        public uint[] SubmeshGpuIds;
        public uint[] MaterialIds;
        public ID3D12PipelineState[] GPassPsos;
        public ID3D12PipelineState[] DepthPsos;
    }
}
