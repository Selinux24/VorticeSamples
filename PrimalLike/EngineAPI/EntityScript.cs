
namespace PrimalLike.EngineAPI
{
    public class EntityScript : Entity
    {
        public EntityScript()
        {
        }
        public EntityScript(Entity entity) : base(entity.Id)
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
