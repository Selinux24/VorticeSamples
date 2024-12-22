using PrimalLike.EngineAPI;
using System;

namespace PrimalLike.Components
{
    public struct ScriptInfo()
    {
        public Func<Entity, EntityScript> ScriptCreator { get; set; }
    }
}
