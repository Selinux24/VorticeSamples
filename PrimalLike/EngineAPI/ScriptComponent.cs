using PrimalLike.Common;

namespace PrimalLike.EngineAPI
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
