global using EntityId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrimalLike.Components
{
    static class GameEntity
    {
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<EntityId> freeIds = [];

        internal static List<TransformComponent> Transforms { get; } = [];
        internal static List<ScriptComponent> Scripts { get; } = [];
        internal static List<GeometryComponent> Geometries { get; } = [];

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
                Transforms.Add(default);
                Scripts.Add(default);
                Geometries.Add(default);
            }

            Entity entity = new(id);
            IdType index = IdDetail.Index(id);

            Debug.Assert(Transforms[(int)index] == null || !Transforms[(int)index].IsValid());
            Transforms[(int)index] = Transform.Create(info.Transform, entity);
            Debug.Assert(Transforms[(int)index].IsValid());

            if (info.Script != null && info.Script?.ScriptCreator != null)
            {
                Debug.Assert(Scripts[(int)index] == null || !Scripts[(int)index].IsValid());
                Scripts[(int)index] = Script.Create(info.Script.Value, entity);
                Debug.Assert(Scripts[(int)index].IsValid());
            }

            if (info.Geometry != null)
            {
                Debug.Assert(Geometries[(int)index] == null || !Geometries[(int)index].IsValid());
                Geometries[(int)index] = Geometry.Create(info.Geometry.Value, entity);
                Debug.Assert(Geometries[(int)index].IsValid());
            }

            return entity;
        }
        public static void Remove(EntityId id)
        {
            IdType index = IdDetail.Index(id);
            Debug.Assert(IsAlive(id));

            if (Scripts[(int)index]?.IsValid() ?? false)
            {
                Script.Remove(Scripts[(int)index]);
                Scripts[(int)index] = new();
            }

            if (Geometries[(int)index]?.IsValid() ?? false)
            {
                Geometry.Remove(Geometries[(int)index]);
                Geometries[(int)index] = new();
            }

            if (Transforms[(int)index].IsValid())
            {
                Transform.Remove(Transforms[(int)index]);
                Transforms[(int)index] = new();
            }

            if (generations[(int)index] < GenerationType.MaxValue)
            {
                freeIds.Enqueue(id);
            }
        }
        public static bool IsAlive(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id), "The entity id is not valid.");
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count, "Index is outside of the generation list bounds.");
            return generations[(int)index] ==
                IdDetail.Generation(id) &&
                Transforms[(int)index].IsValid();
        }
    }
}
