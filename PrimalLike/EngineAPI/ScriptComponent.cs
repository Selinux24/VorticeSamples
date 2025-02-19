using PrimalLike.Common;

namespace PrimalLike.EngineAPI
{
    public struct ScriptComponent
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

        public readonly bool IsValid()
        {
            return IdDetail.IsValid(Id);
        }
    }
}
