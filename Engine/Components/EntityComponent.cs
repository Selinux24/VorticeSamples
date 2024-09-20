global using EntityId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Components
{
    public static class EntityComponent
    {
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<EntityId> freeIds = [];

        public static List<Transform> Transforms { get; } = [];
        public static List<Geometry> Geometries { get; } = [];

        public static Entity Create(EntityInfo info)
        {
            EntityId id;
            if (freeIds.Count > IdDetail.MinDeletedElements)
            {
                id = freeIds.Dequeue();
                Debug.Assert(!IsAlive(id));
                id = IdDetail.NewGeneration(id);
                ++generations[(int)IdDetail.Index(id)];
            }
            else
            {
                id = (EntityId)generations.Count;
                generations.Add(0);
                Transforms.Add(new());
                Geometries.Add(new());
            }

            Entity entity = new(id);
            IdType index = IdDetail.Index(id);

            Transforms[(int)index] = TransformComponent.Create(info.TransformInfo, entity);
            Geometries[(int)index] = GeometryComponent.Create(info.GeometryInfo, entity);

            return entity;
        }
        public static void Remove(EntityId id)
        {
            IdType index = IdDetail.Index(id);
            Debug.Assert(IsAlive(id));

            if (Transforms[(int)index].IsValid())
            {
                TransformComponent.Remove(Transforms[(int)index]);
                Transforms[(int)index] = new();
            }

            if (Geometries[(int)index].IsValid())
            {
                GeometryComponent.Remove(Geometries[(int)index]);
                Geometries[(int)index] = new();
            }

            if (generations[(int)index] < GenerationType.MaxValue)
            {
                freeIds.Enqueue(id);
            }
        }
        public static bool IsAlive(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count);
            return generations[(int)index] == 
                IdDetail.Generation(id) && 
                Transforms[(int)index].IsValid() &&
                Geometries[(int)index].IsValid();
        }
    }
}
