using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using System.Collections.Generic;

namespace PrimalLikeDLL
{
    class ViewportSurface() : RenderSurface()
    {
        public Camera Camera = new();
        public readonly List<uint> GeometryIds = [];
    }
}
