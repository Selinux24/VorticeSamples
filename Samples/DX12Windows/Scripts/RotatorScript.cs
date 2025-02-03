using PrimalLike.EngineAPI;
using System.Numerics;
using Vortice.Mathematics;

namespace DX12Windows.Scripts
{
    public class RotatorScript : EntityScript
    {
        private float angle = 0f;

        public RotatorScript() : base()
        {
        }
        public RotatorScript(Entity entity) : base(entity)
        {
        }

        public override void Update(float deltaTime)
        {
            angle += 0.25f * deltaTime * MathHelper.TwoPi;
            if (angle > MathHelper.TwoPi)
            {
                angle -= MathHelper.TwoPi;
            }
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(angle, 0, 0);
            SetRotation(rotation);
        }

        public override string ToString()
        {
            return $"Id: {Id}; Script: {Script.Id}";
        }
    }
}
