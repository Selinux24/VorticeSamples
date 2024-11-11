using Engine.Common;
using Engine.Components;
using Engine.EngineAPI;
using System.Diagnostics;

namespace EngineDLL
{
    public static class EntityAPI
    {
        public static Entity EntityFromId(uint id)
        {
            return new Entity(id);
        }

        public static uint CreateGameEntity(GameEntityDescriptor descriptor)
        {
            TransformInfo transformInfo = descriptor.Transform.ToTransformInfo();
            ScriptInfo scriptInfo = descriptor.Script.ToScriptInfo();
            EntityInfo info = new()
            {
                Transform = transformInfo,
                Script = scriptInfo
            };

            return GameEntity.Create(info).Id;
        }

        public static void RemoveGameEntity(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            GameEntity.Remove(id);
        }
    }
}
