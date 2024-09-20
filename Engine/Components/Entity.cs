using Engine.Common;
using System.Diagnostics;
using System.Numerics;

namespace Engine.Components
{
    public class Entity
    {
        public EntityId Id { get; private set; }

        public Transform Transform
        {
            get
            {
                Debug.Assert(IsValid());
                return EntityComponent.Transforms[(int)IdDetail.Index(Id)];
            }
        }
        public Quaternion Rotation
        {
            get
            {
                return Transform.Rotation;
            }
        }
        public Vector3 Orientation
        {
            get
            {
                return Transform.Orientation;
            }
        }
        public Vector3 Position
        {
            get
            {
                return Transform.Position;
            }
        }
        public Vector3 Scale
        {
            get
            {
                return Transform.Scale;
            }
        }

        public Geometry Geometry
        {
            get
            {
                Debug.Assert(IsValid());
                return EntityComponent.Geometries[(int)IdDetail.Index(Id)];
            }
        }

        public Entity()
        {
            Id = EntityId.MaxValue;
        }
        public Entity(EntityId id)
        {
            Id = id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }

    public struct EntityInfo()
    {
        public TransformInfo TransformInfo { get; set; } = new();
        public GeometryInfo GeometryInfo { get; set; } = new();
    }
}
