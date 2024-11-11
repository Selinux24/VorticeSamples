using System;
using Engine.EngineAPI;

namespace Engine.Components
{
    public struct ScriptInfo()
    {
        public Func<Entity, EntityScript> ScriptCreator { get; set; }
    }
}
