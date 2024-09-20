using Engine.Common;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    public class Transform
    {
        public TransformId Id { get; private set; }
        public Quaternion Rotation
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Rotations[(int)IdDetail.Index(Id)];
            }
        }
        public Vector3 Orientation
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Orientations[(int)IdDetail.Index(Id)];
            }
        }
        public Vector3 Position
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Positions[(int)IdDetail.Index(Id)];
            }
        }
        public Vector3 Scale
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Scales[(int)IdDetail.Index(Id)];
            }
        }

        public Transform()
        {
            Id = TransformId.MaxValue;
        }
        public Transform(TransformId id)
        {
            Id = id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }

    public struct TransformInfo()
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;
    }

    public enum TransformFlags : uint
    {
        Rotation = 0x01,
        Orientation = 0x02,
        Position = 0x04,
        Scale = 0x08,

        All = Rotation | Orientation | Position | Scale
    }

    struct TransformCache
    {
        public Quaternion Rotation { get; set; }
        public Vector3 Orientation { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public TransformId Id { get; set; }
        public TransformFlags Flags { get; set; }
    };
}
