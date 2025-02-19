using PrimalLike.Common;
using PrimalLike.Components;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public struct TransformComponent
    {
        public TransformId Id { get; private set; }
        public readonly Quaternion Rotation
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Rotations[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Orientation
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Orientations[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Position
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Positions[(int)IdDetail.Index(Id)];
            }
        }
        public readonly Vector3 Scale
        {
            get
            {
                Debug.Assert(IsValid());
                return Transform.Scales[(int)IdDetail.Index(Id)];
            }
        }

        public TransformComponent()
        {
            Id = TransformId.MaxValue;
        }
        public TransformComponent(TransformId id)
        {
            Id = id;
        }

        public readonly bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
