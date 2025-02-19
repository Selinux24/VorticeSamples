using PrimalLike.Common;

namespace PrimalLike.EngineAPI
{
    public struct GeometryComponent
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

        public readonly bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
