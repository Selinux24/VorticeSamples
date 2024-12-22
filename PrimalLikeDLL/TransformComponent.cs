using PrimalLike.Components;
using System.Numerics;

namespace PrimalLikeDLL
{
    public struct TransformComponent
    {
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public TransformInfo ToTransformInfo()
        {
            return new TransformInfo()
            {
                Position = Position,
                Rotation = Quaternion.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z),
                Scale = Scale
            };
        }
    }
}
