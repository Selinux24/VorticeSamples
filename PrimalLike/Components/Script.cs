global using ScriptId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrimalLike.Components
{
    static class Script
    {
        private static readonly List<EntityScript> entityScripts = [];
        private static readonly List<IdType> idMapping = [];
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<ScriptId> freeIds = [];
        private static readonly Dictionary<string, Func<Entity, EntityScript>> scriptRegistry = [];

        private static bool Exists(ScriptId id)
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

        public static bool RegisterScript(string tag, Func<Entity, EntityScript> func)
        {
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
                id = (ScriptId)generations.Count;
                idMapping.Add(default);
                generations.Add(0);
            }

            Debug.Assert(IdDetail.IsValid(id));
            IdType index = (IdType)entityScripts.Count;
            entityScripts.Add(info.ScriptCreator(entity));
            Debug.Assert(entityScripts[^1].Id == entity.Id);
            idMapping[(int)IdDetail.Index(id)] = index;

            return new(entity.Id);
        }
        public static void Remove(ScriptComponent c)
        {
            Debug.Assert(c.IsValid() && Exists(c.Id));
            ScriptId id = c.Id;
            IdType index = idMapping[(int)IdDetail.Index(id)];
            ScriptId lastId = entityScripts[^1].Id;

            //Erase unordered
            entityScripts[(int)index] = entityScripts[^1];
            entityScripts.RemoveAt(entityScripts.Count - 1);

            idMapping[(int)IdDetail.Index(lastId)] = index;
            idMapping[(int)IdDetail.Index(id)] = IdDetail.InvalidId;

            if (generations[(int)index] < GenerationType.MaxValue)
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
        }

#if EDITOR
        private static readonly List<string> scriptNames = [];
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
