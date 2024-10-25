
namespace Engine.Components
{
    public class Script : Entity
    {
        public Script() : base(ScriptId.MaxValue)
        {
        }
        public Script(Entity entity) : base(entity.Id)
        {
        }

        public virtual void BeginPlay()
        {
        }
        public virtual void Update(float dt)
        {
        }
    }
}
