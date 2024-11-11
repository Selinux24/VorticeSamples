using Engine.Common;

namespace Engine.EngineAPI
{
    public class ScriptComponent
    {
        public ScriptId Id { get; private set; }

        public ScriptComponent()
        {
            Id = ScriptId.MaxValue;
        }
        public ScriptComponent(ScriptId id)
        {
            Id = id;
        }

        public bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
