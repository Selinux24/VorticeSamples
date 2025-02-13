using PrimalLike;
using PrimalLike.EngineAPI;
using System;
using System.Numerics;
using Vortice.Mathematics;

namespace DX12Windows.Scripts
{
    public class CameraScript : EntityScript
    {
        readonly InputSystem<CameraScript> inputSystem = new();

        Vector3 desiredPosition;
        Vector3 desiredSpherical;
        Vector3 position;
        Vector3 spherical;
        Vector3 move = Vector3.Zero;
        float moveMagnitude = 0f;
        float dt;
        float positionAcceleration = 0f;
        bool movePosition = false;
        bool moveRotation = false;

        public CameraScript() : base()
        {
        }
        public CameraScript(Entity entity) : base(entity)
        {
            inputSystem.AddHandler(InputSources.Mouse, this, MouseMove);
            inputSystem.AddHandler((ulong)"move".GetHashCode(), this, OnMove);

            desiredPosition = position = Position;

            Vector3 dir = Orientation;
            float theta = MathF.Acos(dir.Y);
            float phi = MathF.Atan2(-dir.Z, dir.X);
            Vector3 rot = new(theta - MathHelper.PiOver2, phi + MathHelper.PiOver2, 0f);
            desiredSpherical = spherical = rot;
        }

        public override void Update(float deltaTime)
        {
            dt = deltaTime;

            if (moveMagnitude > float.Epsilon)
            {
                float fpsScale = dt / 0.016667f;
                Quaternion rot = Rotation;
                Vector3 d = Vector3.Transform(move * 0.05f * fpsScale, rot);
                if (positionAcceleration < 1f) positionAcceleration += 0.02f * fpsScale;
                desiredPosition += d * positionAcceleration;
                movePosition = true;
            }
            else if (movePosition)
            {
                positionAcceleration = 0f;
            }

            if (movePosition || moveRotation)
            {
                CameraSeek(dt);
            }
        }
        private void MouseMove(InputSources type, InputCodes code, ref InputValue mousePos)
        {
            if (code != InputCodes.MousePosition)
            {
                return;
            }

            Input.Get(InputSources.Mouse, InputCodes.MouseLeft, out var value);
            if (value.Current.Z == 0f) return;

            const float scale = 0.005f;
            float dx = (mousePos.Current.X - mousePos.Previous.X) * scale;
            float dy = (mousePos.Current.Y - mousePos.Previous.Y) * scale;

            Vector3 spherical = desiredSpherical;
            spherical.X += dy;
            spherical.Y -= dx;
            spherical.X = Math.Clamp(spherical.X, 0.0001f - MathHelper.PiOver2, MathHelper.PiOver2 - 0.0001f);

            desiredSpherical = spherical;
            moveRotation = true;
        }
        private void OnMove(ulong binding, ref InputValue value)
        {
            move = value.Current;
            moveMagnitude = move.Length();
        }
        private void CameraSeek(float dt)
        {
            Vector3 p = desiredPosition - position;
            Vector3 o = desiredSpherical - spherical;

            movePosition = p.LengthSquared() > float.Epsilon;
            moveRotation = o.LengthSquared() > float.Epsilon;

            float scale = 0.2f * dt / 0.016667f;

            if (movePosition)
            {
                position += p * scale;
                SetPosition(position);
            }

            if (moveRotation)
            {
                spherical += o * scale;
                Vector3 newRot = spherical;
                newRot.X = Math.Clamp(newRot.X, 0.0001f - MathHelper.PiOver2, MathHelper.PiOver2 - 0.0001f);
                spherical = newRot;

                Quaternion quat = Quaternion.CreateFromYawPitchRoll(spherical.Y, spherical.X, spherical.Z);
                SetRotation(quat);
            }
        }
    }
}
