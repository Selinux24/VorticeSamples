using System.Numerics;

namespace PrimalLike.Components
{
    public struct TransformInfo()
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;
    }
}
