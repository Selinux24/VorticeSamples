using System;

namespace PrimalLike.Graphics
{
    [Flags]
    public enum ShaderFlags : uint
    {
        None = 0x0,
        Vertex = 0x01,
        Hull = 0x02,
        Domain = 0x04,
        Geometry = 0x08,
        Pixel = 0x10,
        Compute = 0x20,
        Amplification = 0x40,
        Mesh = 0x80,
    }
}
