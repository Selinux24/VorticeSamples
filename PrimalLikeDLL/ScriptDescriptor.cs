using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System;

namespace PrimalLikeDLL
{
    public struct ScriptDescriptor
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
