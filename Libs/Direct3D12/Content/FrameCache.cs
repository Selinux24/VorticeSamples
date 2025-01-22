using PrimalLike.Content;
using System.Collections.Generic;

namespace Direct3D12.Content
{
    struct FrameCache()
    {
        public List<LodOffset> LodOffsets = [];
        public List<uint> GeometryIds = [];
    }
}
