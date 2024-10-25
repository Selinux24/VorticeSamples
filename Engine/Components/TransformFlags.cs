
namespace Engine.Components
{
    public enum TransformFlags : uint
    {
        Rotation = 0x01,
        Orientation = 0x02,
        Position = 0x04,
        Scale = 0x08,

        All = Rotation | Orientation | Position | Scale
    }
}
