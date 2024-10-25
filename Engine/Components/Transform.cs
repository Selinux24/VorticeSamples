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
        public Transform(Entity entity)
        {
            Id = entity.Id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
