global using EntityId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Components
{
    public static class EntityComponent
    {
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<GenerationType> freeIds = [];

        public static List<Transform> Transforms { get; } = [];

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
            }

            Entity entity = new(id);
            IdType index = IdDetail.Index(id);

            Transforms[(int)index] = TransformComponent.Create(info.TransformInfo, entity);

            return entity;
        }
        public static void Remove(EntityId id)
        {
            IdType index = IdDetail.Index(id);
            Debug.Assert(IsAlive(id));

            TransformComponent.Remove(Transforms[(int)index]);
            Transforms[(int)index] = new();
        }
        public static bool IsAlive(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count);
            return generations[(int)index] == IdDetail.Generation(id) && Transforms[(int)index].IsValid();
        }
    }
}
