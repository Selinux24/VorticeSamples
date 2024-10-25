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
        public Geometry(Entity entity)
        {
            Id = entity.Id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
