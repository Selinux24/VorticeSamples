global using ScriptId = uint;
using Engine.Common;
using System.Collections.Generic;
using System.Diagnostics;

namespace Engine.Components
{
    public static class ScriptComponent
    {
        private static readonly List<Entity> entityScripts = [];
        private static readonly List<IdType> idMapping = [];
        private static readonly List<GenerationType> generations = [];
        private static readonly Queue<ScriptId> freeIds = [];

        private static bool Exists(ScriptId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count && !(IdDetail.IsValid(idMapping[(int)index]) && idMapping[(int)index] >= entityScripts.Count));
            Debug.Assert(generations[(int)index] == IdDetail.Generation(id));
            return (generations[(int)index] == IdDetail.Generation(id)) &&
                IdDetail.IsValid(idMapping[(int)index]) &&
                true /*IdDetail.IsValid(entityScripts[(int)idMapping[(int)index]])*/;
        }

        public static Script Create(ScriptInfo info, Entity entity)
        {
            Debug.Assert(entity.IsValid());
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

            return new(entity);
        }
        public static void Remove(Script c)
        {
            Debug.Assert(c.IsValid() && Exists(c.Id));
            ScriptId id = c.Id;
            IdType index = idMapping[(int)IdDetail.Index(id)];
            ScriptId last_id = entityScripts[^1].Script.Id;
            
            //Erase unordered
            entityScripts[(int)index] = entityScripts[^1];
            entityScripts.RemoveAt(entityScripts.Count - 1);

            idMapping[(int)IdDetail.Index(last_id)] = index;
            idMapping[(int)IdDetail.Index(id)] = IdType.MaxValue;

            if (generations[(int)index] < GenerationType.MaxValue)
            {
                freeIds.Enqueue(id);
            }
        }
    }
}
