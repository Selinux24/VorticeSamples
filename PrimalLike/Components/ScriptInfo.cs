using PrimalLike.EngineAPI;
using System;

namespace PrimalLike.Components
{
    public struct ScriptInfo()
    {
        public Func<Entity, EntityScript> ScriptCreator { get; set; }

        public ScriptInfo(string scriptName) : this()
        {
            ScriptCreator = Script.GetScriptCreator(scriptName);
        }
    }
}
