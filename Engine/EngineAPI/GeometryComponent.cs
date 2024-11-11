using Engine.Common;

namespace Engine.EngineAPI
{
    public class GeometryComponent
    {
        public GeometryId Id { get; private set; }

        public GeometryComponent()
        {
            Id = GeometryId.MaxValue;
        }
        public GeometryComponent(GeometryId id)
        {
            Id = id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
