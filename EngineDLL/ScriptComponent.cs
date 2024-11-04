using Engine.Components;
using System;

namespace EngineDLL
{
    public struct ScriptComponent
    {
        public Func<Entity, Script> ScriptCreator { get; set; }

        public ScriptInfo ToScriptInfo()
        {
            return new ScriptInfo()
            {
                ScriptCreator = ScriptCreator
            };
        }
    }
}
