using Engine.Common;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    public class Entity(EntityId id)
    {
        public EntityId Id { get; private set; } = id;
        
        public Transform Transform
        {
            get
            {
                Debug.Assert(IsValid());
                return EntityComponent.Transforms[(int)IdDetail.Index(Id)];
            }
            set
            {
                Debug.Assert(IsValid());
                EntityComponent.Transforms[(int)IdDetail.Index(Id)] = value;
            }
        }
        public Vector3 Position
        {
            get
            {
                Debug.Assert(EntityComponent.IsAlive(Id));
                return Transform.Position;
            }
        }
        public Quaternion Rotation
        {
            get
            {
                Debug.Assert(EntityComponent.IsAlive(Id));
                return Transform.Rotation;
            }
        }
        public Vector3 Scale
        {
            get
            {
                Debug.Assert(EntityComponent.IsAlive(Id));
                return Transform.Scale;
            }
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }

    public struct EntityInfo()
    {
        public TransformInfo TransformInfo { get; set; }
    }
}
