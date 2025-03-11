using PrimalLike.Graphics;
using Vortice.Direct3D12;

namespace Direct3D12.Content
{
    struct MaterialsCache
    {
        public ID3D12RootSignature[] RootSignatures;
        public MaterialTypes[] MaterialTypes;
        public uint[][] DescriptorIndices;
        public uint[] TextureCounts;
    }
}
