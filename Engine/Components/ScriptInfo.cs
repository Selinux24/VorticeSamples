using System;

namespace Engine.Components
{
    public struct ScriptInfo()
    {
        public Func<Entity, Script> ScriptCreator { get; set; }
    }
}
