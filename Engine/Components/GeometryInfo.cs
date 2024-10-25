
namespace Engine.Components
{
    public struct GeometryInfo()
    {
        public IdType GeometryContentId { get; set; } = IdType.MaxValue;
        public IdType[] MaterialIds { get; set; } = [];
    }
}
