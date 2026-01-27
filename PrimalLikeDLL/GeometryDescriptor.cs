using PrimalLike.Components;

namespace PrimalLikeDLL
{
    public struct GeometryDescriptor
    {
        public uint GeometryContentId;
        public uint[] MaterialIds;

        public GeometryInfo ToGeometryInfo()
        {
            return new GeometryInfo()
            {
                GeometryContentId = GeometryContentId,
                MaterialIds = MaterialIds
            };
        }
    }
}
