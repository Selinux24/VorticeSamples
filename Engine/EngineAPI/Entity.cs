using Engine.Common;
using Engine.Components;
using System.Diagnostics;

namespace Engine.EngineAPI
{
    public class Entity
    {
        public EntityId Id { get; private set; }

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
}
