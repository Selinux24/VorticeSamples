using PrimalLike;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
        : Application(Path.Combine(ContentFolder, "Game.bin"), platformFactory, graphicsFactory)
    {
        const string ContentFolder = "./Content/";

        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            if (!Directory.Exists(ContentFolder))
            {
                Directory.CreateDirectory(ContentFolder);
            }

            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }

        public static Entity CreateOneGameEntity(Vector3 position, Quaternion rotation)
        {
            return CreateOneGameEntityInternal(position, rotation, 1.0f, null);
        }
        public static Entity CreateOneGameEntity(Vector3 position, Quaternion rotation, float scale)
        {
            return CreateOneGameEntityInternal(position, rotation, scale, null);
        }
        public static Entity CreateOneGameEntity(Vector3 position, Quaternion rotation, GeometryInfo geometryInfo)
        {
            return CreateOneGameEntityInternal(position, rotation, 1.0f, geometryInfo);
        }
        public static Entity CreateOneGameEntity(Vector3 position, Quaternion rotation, float scale, GeometryInfo geometryInfo)
        {
            return CreateOneGameEntityInternal(position, rotation, scale, geometryInfo);
        }
        static Entity CreateOneGameEntityInternal(Vector3 position, Quaternion rotation, float scale, GeometryInfo? geometryInfo)
        {
            EntityInfo entityInfo = new()
            {
                Geometry = geometryInfo,
                Transform = new()
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = new(scale),
                },
            };

            Entity ntt = CreateEntity(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }

        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation) where T : EntityScript
        {
            return CreateOneGameEntityInternal<T>(position, rotation, 1.0f, null);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation, float scale) where T : EntityScript
        {
            return CreateOneGameEntityInternal<T>(position, rotation, scale, null);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation, GeometryInfo geometryInfo) where T : EntityScript
        {
            return CreateOneGameEntityInternal<T>(position, rotation, 1.0f, geometryInfo);
        }
        public static Entity CreateOneGameEntity<T>(Vector3 position, Quaternion rotation, float scale, GeometryInfo geometryInfo) where T : EntityScript
        {
            return CreateOneGameEntityInternal<T>(position, rotation, scale, geometryInfo);
        }
        static Entity CreateOneGameEntityInternal<T>(Vector3 position, Quaternion rotation, float scale, GeometryInfo? geometryInfo) where T : EntityScript
        {
            if (!RegisterScript<T>())
            {
                Console.WriteLine($"Failed to register script {nameof(T)}");
            }

            EntityInfo entityInfo = new()
            {
                Geometry = geometryInfo,
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

        public static void RemoveGameEntity(uint id)
        {
            RemoveEntity(id);
        }
    }
}
