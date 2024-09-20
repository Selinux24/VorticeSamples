global using GeometryId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Components
{
    public static class GeometryComponent
    {
        private static readonly List<uint> activeLod = [];
        private static readonly List<IdType> renderItemIds = [];
        private static readonly List<GeometryId> ownerIds = [];
        private static readonly List<IdType> idMapping = [];
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<GeometryId> freeIds = [];

        private static bool Exists(GeometryId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count && !(IdDetail.IsValid(idMapping[(int)index]) && idMapping[(int)index] >= renderItemIds.Count));
            Debug.Assert(generations[(int)index] == IdDetail.Generation(id));
            return (generations[(int)index] == IdDetail.Generation(id)) &&
                IdDetail.IsValid(idMapping[(int)index]) &&
                IdDetail.IsValid(renderItemIds[(int)idMapping[(int)index]]);
        }

        public static Geometry Create(GeometryInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid());
            Debug.Assert(IdDetail.IsValid(info.GeometryContentId) && info.MaterialIds.Length > 0);

            GeometryId id;
            if (freeIds.Count > IdDetail.MinDeletedElements)
            {
                id = freeIds.Dequeue();
                Debug.Assert(!Exists(id));
                id = IdDetail.NewGeneration(id);
                ++generations[(int)IdDetail.Index(id)];
            }
            else
            {
                id = (GeometryId)generations.Count;
                generations.Add(0);
                idMapping.Add(0);
            }

            IdType index = (IdType)renderItemIds.Count;
            activeLod.Add(0);
            renderItemIds.Add(0);
            ownerIds.Add(IdDetail.Index(id));
            idMapping[(int)IdDetail.Index(id)] = index;

            return new(id);
        }
        public static void Remove(Geometry c)
        {
            Debug.Assert(c.IsValid() && Exists(c.Id));
            GeometryId id = c.Id;
            IdType index = idMapping[(int)IdDetail.Index(id)];
            GeometryId last_id = ownerIds[^1];
            //TODO Remove graphics content
            idMapping[(int)IdDetail.Index(last_id)] = index;
            idMapping[(int)IdDetail.Index(id)] = IdType.MaxValue;

            if (generations[(int)index] < GenerationType.MaxValue)
            {
                freeIds.Enqueue(id);
            }
        }
        public static IdType[] GetRenderItemIds()
        {
            return [.. renderItemIds];
        }
    }
}
