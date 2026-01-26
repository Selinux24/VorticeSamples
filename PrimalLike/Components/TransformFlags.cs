using System;

namespace PrimalLike.Components
{
    [Flags]
    public enum TransformFlags : uint
    {
        Rotation = 0x01,
        Position = 0x02,
        Scale = 0x04,

        All = Rotation | Position | Scale
    }
}
