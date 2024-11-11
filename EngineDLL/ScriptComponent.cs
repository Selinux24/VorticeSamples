using Engine.Components;
using Engine.EngineAPI;
using System;

namespace EngineDLL
{
    public struct ScriptComponent
    {
        public Func<Entity, EntityScript> ScriptCreator { get; set; }

        public ScriptInfo ToScriptInfo()
        {
            return new ScriptInfo()
            {
                ScriptCreator = ScriptCreator
            };
        }
    }
}
