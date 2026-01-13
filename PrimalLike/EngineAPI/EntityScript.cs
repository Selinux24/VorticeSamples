using System.Numerics;

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

        protected void SetRotation(Quaternion rotation)
        {
            Components.Script.SetRotation(this, rotation);
        }
        protected void SetOrientation(Vector3 orientation)
        {
            Components.Script.SetOrientation(this, orientation);
        }
        protected void SetPosition(Vector3 position)
        {
            Components.Script.SetPosition(this, position);
        }
        protected void SetScale(Vector3 scale)
        {
            Components.Script.SetScale(this, scale);
        }
    }
}
