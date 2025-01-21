using System.Numerics;

namespace PrimalLike.Components
{
    public struct TransformCache
    {
        public Quaternion Rotation { get; set; }
        public Vector3 Orientation { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public TransformId Id { get; set; }
        public TransformFlags Flags { get; set; }
    }
}
