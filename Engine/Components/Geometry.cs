using Engine.Common;

namespace Engine.Components
{
    public class Geometry
    {
        public GeometryId Id { get; private set; }

        public Geometry()
        {
            Id = GeometryId.MaxValue;
        }
        public Geometry(GeometryId id)
        {
            Id = id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }

    public struct GeometryInfo()
    {
        public IdType GeometryContentId { get; set; } = IdType.MaxValue;
        public IdType[] MaterialIds { get; set; } = [];
    }
}
