using PrimalLike;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using System;
using System.Diagnostics;
using System.Numerics;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
        : Application("Content/Game.bin", platformFactory, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }

        public static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation)
        {
            return CreateOneGameEntity(position, rotation, 1.0f);
        }
        public static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, float scale)
        {
            EntityInfo entityInfo = new()
            {
                Transform = new()
                {
                    Position = position,
                    Rotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z),
                    Scale = new(scale),
                },
            };

            Entity ntt = CreateEntity(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }

        public static Entity CreateOneGameEntity<T>(Vector3 position, Vector3 rotation) where T : EntityScript
        {
            return CreateOneGameEntity<T>(position, rotation, 1.0f);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation) where T : EntityScript
        {
            return CreateOneGameEntity<T>(position, rotation, 1.0f);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Vector3 rotation, float scale) where T : EntityScript
        {
            var q = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
            return CreateOneGameEntity<T>(position, q, scale);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation, float scale) where T : EntityScript
        {
            if (!RegisterScript<T>())
            {
                Console.WriteLine($"Failed to register script {nameof(T)}");
            }

            EntityInfo entityInfo = new()
            {
                Transform = new()
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = new(scale),
                },
                Script = new()
                {
                    ScriptCreator = Script.GetScriptCreator<T>(),
                }
            };

            Entity ntt = CreateEntity(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }
    }
}
