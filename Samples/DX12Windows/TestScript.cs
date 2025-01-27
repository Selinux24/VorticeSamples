using PrimalLike.EngineAPI;

namespace DX12Windows
{
    public class TestScript : EntityScript
    {
        public TestScript() : base()
        {
        }
        public TestScript(Entity entity) : base(entity)
        {
        }

        public override void Update(float deltaTime)
        {
        }

        public override string ToString()
        {
            return $"Id: {Id}; Script: {Script.Id}";
        }
    }
}
