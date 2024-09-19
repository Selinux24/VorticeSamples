using Engine.Common;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    public class Transform(TransformId id)
    {
        public TransformId Id { get; private set; } = id;
        public Vector3 Position
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Positions[(int)IdDetail.Index(Id)];
            }
            set
            {
                Debug.Assert(IsValid());
                TransformComponent.Positions[(int)IdDetail.Index(Id)] = value;
            }
        }
        public Quaternion Rotation
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Rotations[(int)IdDetail.Index(Id)];
            }
            set
            {
                Debug.Assert(IsValid());
                TransformComponent.Rotations[(int)IdDetail.Index(Id)] = value;
            }
        }
        public Vector3 Scale
        {
            get
            {
                Debug.Assert(IsValid());
                return TransformComponent.Scales[(int)IdDetail.Index(Id)];
            }
            set
            {
                Debug.Assert(IsValid());
                TransformComponent.Scales[(int)IdDetail.Index(Id)] = value;
            }
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
}
