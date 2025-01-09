using PrimalLike.Common;

namespace PrimalLike.Components
{
    public struct GeometryInfo()
    {
        public IdType GeometryContentId { get; set; } = IdDetail.InvalidId;
        public IdType[] MaterialIds { get; set; } = [];
    }
}
