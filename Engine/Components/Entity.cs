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
                Debug.Assert(EntityComponent.IsAlive(Id), $"The transform's entity id {Id} is not alive.");
                return EntityComponent.Transforms[(int)IdDetail.Index(Id)];
            }
        }
        public Script Script
        {
            get
            {
                Debug.Assert(EntityComponent.IsAlive(Id), $"The script's entity id {Id} is not alive.");
                return EntityComponent.Scripts[(int)IdDetail.Index(Id)];
            }
        }
        public Geometry Geometry
        {
            get
            {
                Debug.Assert(EntityComponent.IsAlive(Id), $"The geometry's entity id {Id} is not alive.");
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
}
