using PrimalLike.Common;
using PrimalLike.Components;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public class Entity
    {
        readonly EntityId id;

        public Entity()
        {
            id = EntityId.MaxValue;
        }
        public Entity(EntityId id)
        {
            this.id = id;
        }

        public EntityId Id { get => id; }
        public bool IsValid { get => IdDetail.IsValid(id); }

        public TransformComponent Transform
        {
            get
            {
                Debug.Assert(GameEntity.IsAlive(Id), $"The transform's entity id {Id} is not alive.");
                return GameEntity.GetTransform(IdDetail.Index(Id));
            }
        }
        public ScriptComponent Script
        {
            get
            {
                Debug.Assert(GameEntity.IsAlive(Id), $"The script's entity id {Id} is not alive.");
                return GameEntity.GetScript(IdDetail.Index(Id));
            }
        }
        public GeometryComponent Geometry
        {
            get
            {
                Debug.Assert(GameEntity.IsAlive(Id), $"The geometry's entity id {Id} is not alive.");
                return GameEntity.GetGeometry(IdDetail.Index(Id));
            }
        }

        public Quaternion Rotation { get => Transform.Rotation; }
        public Vector3 Orientation { get => Transform.Orientation; }
        public Vector3 Position { get => Transform.Position; }
        public Vector3 Scale { get => Transform.Scale; }
    }
}
