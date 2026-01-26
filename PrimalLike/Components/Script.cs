global using ScriptId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike.Components
{
    public static class Script
    {
        static readonly List<EntityScript> entityScripts = [];
        static readonly List<IdType> idMapping = [];
        static readonly List<GenerationType> generations = [];
        static readonly Queue<ScriptId> freeIds = [];
        static readonly List<TransformCache> transformCache = [];
        static readonly Dictionary<IdType, uint> cacheMap = [];

        static readonly Dictionary<string, Func<Entity, EntityScript>> scriptRegistry = [];

        static bool Exists(ScriptId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count && !(IdDetail.IsValid(idMapping[(int)index]) && idMapping[(int)index] >= entityScripts.Count));
            Debug.Assert(generations[(int)index] == IdDetail.Generation(id));
            return IdDetail.IsValid(idMapping[(int)index]) &&
                generations[(int)index] == IdDetail.Generation(id) &&
                entityScripts[(int)idMapping[(int)index]] != null &&
                entityScripts[(int)idMapping[(int)index]].IsValid;
        }
        static uint GetCache(Entity entity)
        {
            Debug.Assert(GameEntity.IsAlive(entity.Id));
            TransformId id = entity.Transform.Id;

            uint index;
            if (cacheMap.TryGetValue(id, out uint cacheIndex))
            {
                index = cacheIndex;
            }
            else
            {
                index = (uint)transformCache.Count;
                transformCache.Add(new() { Id = id });
                cacheMap.Add(id, index);
            }

            Debug.Assert(index < transformCache.Count);
            return index;
        }
     
        public static void SetRotation(Entity entity, Quaternion rotation)
        {
            uint index = GetCache(entity);
            var cache = transformCache[(int)index];
            cache.Flags |= TransformFlags.Rotation;
            cache.Rotation = rotation;
            transformCache[(int)index] = cache;
        }
        public static void SetPosition(Entity entity, Vector3 position)
        {
            uint index = GetCache(entity);
            var cache = transformCache[(int)index];
            cache.Flags |= TransformFlags.Position;
            cache.Position = position;
            transformCache[(int)index] = cache;
        }
        public static void SetScale(Entity entity, Vector3 scale)
        {
            uint index = GetCache(entity);
            var cache = transformCache[(int)index];
            cache.Flags |= TransformFlags.Scale;
            cache.Scale = scale;
            transformCache[(int)index] = cache;
        }

        public static bool RegisterScript(string tag, Func<Entity, EntityScript> func)
        {
            if (scriptRegistry.ContainsKey(tag))
            {
                return true;
            }

            bool result = scriptRegistry.TryAdd(tag, func);
            Debug.Assert(result);
            return result;
        }
        public static Func<Entity, EntityScript> GetScriptCreator(string tag)
        {
            bool result = scriptRegistry.TryGetValue(tag, out Func<Entity, EntityScript> func);
            Debug.Assert(result);
            return func;
        }
        public static Func<Entity, EntityScript> GetScriptCreator<T>() where T : EntityScript
        {
            return GetScriptCreator(IdDetail.StringHash<T>());
        }

        public static ScriptComponent Create(ScriptInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid);
            Debug.Assert(info.ScriptCreator != null);

            ScriptId id;
            if (freeIds.Count > IdDetail.MinDeletedElements)
            {
                id = freeIds.Dequeue();
                Debug.Assert(!Exists(id));
                id = IdDetail.NewGeneration(id);
                ++generations[(int)IdDetail.Index(id)];
            }
            else
            {
                id = (ScriptId)idMapping.Count;
                idMapping.Add(default);
                generations.Add(0);
            }

            Debug.Assert(IdDetail.IsValid(id));
            IdType index = (IdType)entityScripts.Count;
            entityScripts.Add(info.ScriptCreator(entity));
            Debug.Assert(entityScripts[^1].Id == entity.Id);
            idMapping[(int)IdDetail.Index(id)] = index;

            return new(id);
        }
        public static void Remove(ScriptComponent c)
        {
            Debug.Assert(c.IsValid() && Exists(c.Id));
            ScriptId id = c.Id;
            IdType index = idMapping[(int)IdDetail.Index(id)];
            ScriptId lastId = entityScripts[^1].Script.Id;

            //Erase unordered
            entityScripts[(int)index] = entityScripts[^1];
            entityScripts.RemoveAt(entityScripts.Count - 1);

            idMapping[(int)IdDetail.Index(lastId)] = index;
            idMapping[(int)IdDetail.Index(id)] = IdDetail.InvalidId;

            if (generations[(int)index] < IdDetail.MaxGeneration)
            {
                freeIds.Enqueue(id);
            }
        }

        public static void Update(float dt)
        {
            foreach (var script in entityScripts)
            {
                script.Update(dt);
            }

            if (transformCache.Count > 0)
            {
                Transform.Update([.. transformCache]);
                transformCache.Clear();

                cacheMap.Clear();
            }
        }

#if EDITOR
        static readonly List<string> scriptNames = [];

        public static bool AddScriptName(string name)
        {
            scriptNames.Add(name);
            return true;
        }
        public static string[] GetScriptNames()
        {
            return [.. scriptNames];
        }
#endif
    }
}
