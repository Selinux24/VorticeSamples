using System.Numerics;

namespace PrimalLike.Components
{
    public struct TransformCache
    {
        public Quaternion Rotation;
        public Vector3 Orientation;
        public Vector3 Position;
        public Vector3 Scale;
        public TransformId Id;
        public TransformFlags Flags;
    }
}
