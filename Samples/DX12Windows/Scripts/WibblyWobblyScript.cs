using PrimalLike.EngineAPI;
using System;
using System.Numerics;
using Vortice.Mathematics;

namespace DX12Windows.Scripts
{
    public class WibblyWobblyScript : EntityScript
    {
        private float angle = 0f;

        public WibblyWobblyScript() : base()
        {
        }
        public WibblyWobblyScript(Entity entity) : base(entity)
        {
        }

        public override void Update(float deltaTime)
        {
            angle -= 0.01f * deltaTime * MathHelper.TwoPi;
            if (angle > MathHelper.TwoPi) angle += MathHelper.TwoPi;
            float x = angle * 2f - MathHelper.Pi;
            float s1 = 0.05f * MathF.Sin(x) * MathF.Sin(MathF.Sin(x / 1.62f) + MathF.Sin(1.62f * x) + MathF.Sin(3.24f * x));
            x = angle;
            float s2 = 0.05f * MathF.Sin(x) * MathF.Sin(MathF.Sin(x / 1.62f) + MathF.Sin(1.62f * x) + MathF.Sin(3.24f * x));

            Quaternion quat = Quaternion.CreateFromYawPitchRoll(s1, 0f, s2);
            SetRotation(quat);
            Vector3 pos = Position;
            pos.Y = 1.3f + 0.2f * MathF.Sin(x) * MathF.Sin(MathF.Sin(x / 1.62f) + MathF.Sin(1.62f * x) + MathF.Sin(3.24f * x));
            SetPosition(pos);
        }

        public override string ToString()
        {
            return $"Id: {Id}; Script: {Script.Id}";
        }
    }
}
