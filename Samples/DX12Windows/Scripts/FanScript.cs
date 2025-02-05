using PrimalLike.EngineAPI;
using System.Numerics;
using Vortice.Mathematics;

namespace DX12Windows.Scripts
{
    public class FanScript : EntityScript
    {
        private float angle = 0f;

        public FanScript() : base()
        {
        }
        public FanScript(Entity entity) : base(entity)
        {
        }

        public override void Update(float deltaTime)
        {
            angle -= 1f * deltaTime * MathHelper.TwoPi;
            if (angle > MathHelper.TwoPi) angle += MathHelper.TwoPi;
            Quaternion quat = Quaternion.CreateFromYawPitchRoll(0f, angle, 0f);
            SetRotation(quat);
        }

        public override string ToString()
        {
            return $"Id: {Id}; Script: {Script.Id}";
        }
    }
}
