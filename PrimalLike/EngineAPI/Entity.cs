using PrimalLike.Common;
using PrimalLike.Components;
using System.Diagnostics;

namespace PrimalLike.EngineAPI
{
    public class Entity
    {
        private readonly EntityId id;

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
                return GameEntity.Transforms[(int)IdDetail.Index(Id)];
            }
        }
        public ScriptComponent Script
        {
            get
            {
                Debug.Assert(GameEntity.IsAlive(Id), $"The script's entity id {Id} is not alive.");
                return GameEntity.Scripts[(int)IdDetail.Index(Id)];
            }
        }
        public GeometryComponent Geometry
        {
            get
            {
                Debug.Assert(GameEntity.IsAlive(Id), $"The geometry's entity id {Id} is not alive.");
                return GameEntity.Geometries[(int)IdDetail.Index(Id)];
            }
        }
    }
}
