global using GeometryId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrimalLike.Components
{
    public static class Geometry
    {
        static readonly List<IdType> renderItemIds = [];
        static readonly List<EntityId> owningEntityIds = [];
        static readonly List<GeometryId> ownerIds = [];
        static readonly List<IdType> idMapping = [];
        static readonly List<GenerationType> generations = [];
        static readonly Queue<GeometryId> freeIds = [];

        static bool Exists(GeometryId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count && !(IdDetail.IsValid(idMapping[(int)index]) && idMapping[(int)index] >= renderItemIds.Count));
            Debug.Assert(generations[(int)index] == IdDetail.Generation(id));
            return (generations[(int)index] == IdDetail.Generation(id)) &&
                IdDetail.IsValid(idMapping[(int)index]) &&
                IdDetail.IsValid(renderItemIds[(int)idMapping[(int)index]]);
        }

        public static GeometryComponent Create(GeometryInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid);
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
                generations.Add(default);
                idMapping.Add(0);
            }

            Debug.Assert(IdDetail.IsValid(id));
            IdType index = (IdType)renderItemIds.Count;
            renderItemIds.Add(AddRenderItem(entity.Id, info));
            ownerIds.Add(IdDetail.Index(id));
            owningEntityIds.Add(entity.Id);
            idMapping[(int)IdDetail.Index(id)] = index;

            return new(id);
        }
        public static void Remove(GeometryComponent c)
        {
            Debug.Assert(c.IsValid() && Exists(c.Id));
            GeometryId id = c.Id;
            IdType index = idMapping[(int)IdDetail.Index(id)];
            GeometryId last_id = ownerIds[^1];
            Renderer.RemoveRenderItem(renderItemIds[(int)index]);
            renderItemIds[(int)index] = renderItemIds[^1];
            owningEntityIds[(int)index] = owningEntityIds[^1];
            ownerIds[(int)index] = last_id;
            idMapping[(int)IdDetail.Index(last_id)] = index;
            idMapping[(int)IdDetail.Index(id)] = IdDetail.InvalidId;

            if (generations[(int)index] < IdDetail.MaxGeneration)
            {
                freeIds.Enqueue(id);
            }
        }
        public static IdType[] GetRenderItemIds()
        {
            return [.. renderItemIds];
        }
        public static IdType[] GetRenderItemIds(IdType[] geometryIds)
        {
            IdType[] itemids = new IdType[geometryIds.Length];
            for (uint i = 0; i < geometryIds.Length; i++)
            {
                GeometryId id = geometryIds[i];
                Debug.Assert(IdDetail.IsValid(id) && Exists(id));
                IdType index = idMapping[(int)IdDetail.Index(id)];
                Debug.Assert(index < renderItemIds.Count && IdDetail.IsValid(renderItemIds[(int)index]));
                itemids[i] = renderItemIds[(int)index];
            }
            return itemids;
        }
        public static IdType[] GetEntityIds(IdType[] geometryIds)
        {
            IdType[] entityIds = new IdType[geometryIds.Length];
            for (uint i = 0; i < geometryIds.Length; i++)
            {
                GeometryId id = geometryIds[i];
                Debug.Assert(IdDetail.IsValid(id) && Exists(id));
                IdType index = idMapping[(int)IdDetail.Index(id)];
                Debug.Assert(index < owningEntityIds.Count && IdDetail.IsValid(owningEntityIds[(int)index]));
                entityIds[i] = owningEntityIds[(int)index];
            }
            return entityIds;
        }

        static IdType AddRenderItem(EntityId entityId, GeometryInfo geometryInfo)
        {
            return Renderer.AddRenderItem(entityId, geometryInfo.GeometryContentId, geometryInfo.MaterialIds);
        }
    }
}
