using PrimalLike;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Vortice.Mathematics;

namespace DX12Windows.Lights
{
    static class LightGenerator
    {
        private const int randMax = 0x7fff;
        private const float invRandMax = 1f / randMax;

        private static readonly ulong leftSet = 0;
        private static readonly ulong rightSet = 1;
        private static readonly List<Light> lights = [];
        private static readonly Random rand = new(37);

        static Vector3 RGBToColor(byte r, byte g, byte b)
        {
            return new()
            {
                X = r / 255f,
                Y = g / 255f,
                Z = b / 255f
            };
        }
        public static void GenerateLights(bool randomLights)
        {
            // LEFT_SET
            LightInitInfo info = new()
            {
                EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero).Id,
                LightType = LightTypes.Directional,
                LightSetKey = leftSet,
                Intensity = 1f,
                Color = RGBToColor(174, 174, 174)
            };
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0)).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));

            // RIGHT_SET
            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero).Id;
            info.LightSetKey = rightSet;
            info.Color = RGBToColor(150, 100, 200);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0)).Id;
            info.Color = RGBToColor(163, 47, 30);
            lights.Add(Application.CreateLight(info));

            if (randomLights)
            {
                float scale1 = 2f;
                Vector3 scale = new(1f * scale1, 0.5f * scale1, 1f * scale1);
                int dim = 13;
                for (int x = -dim; x < dim; x++)
                {
                    for (int y = 0; y < 2 * dim; y++)
                    {
                        for (int z = -dim; z < dim; z++)
                        {
                            CreateLight(
                                new(x * scale.X, y * scale.Y, z * scale.Z),
                                new(3.14f, Random(), 0f),
                                Random() > 0.5f ? LightTypes.Spot : LightTypes.Point,
                                leftSet,
                                true);
                            CreateLight(
                                new(x * scale.X, y * scale.Y, z * scale.Z),
                                new(3.14f, Random(), 0f),
                                Random() > 0.5f ? LightTypes.Spot : LightTypes.Point,
                                rightSet,
                                true);
                        }
                    }
                }
            }
            else
            {
                CreateLight(new(0, -3, 0), new(), LightTypes.Point, leftSet, false);
                CreateLight(new(0, 0.2f, 1f), new(), LightTypes.Point, leftSet, false);
                CreateLight(new(0, 3, 2.5f), new(), LightTypes.Point, leftSet, false);
                CreateLight(new(0, 0.1f, 7), new(0, 3.14f, 0), LightTypes.Spot, leftSet, false);
            }
        }

        static float Random(float min = 0f)
        {
            float v = rand.Next(0, randMax) * invRandMax;

            return MathF.Max(min, v);
        }

        static void CreateLight(Vector3 position, Vector3 rotation, LightTypes type, ulong lightSetKey, bool randomLights)
        {
            uint entityId = HelloWorldApp.CreateOneGameEntity(position, rotation).Id;

            LightInitInfo info = new()
            {
                EntityId = entityId,
                LightType = type,
                LightSetKey = lightSetKey,
                Intensity = 1f,

                Color = new(Random(0.2f), Random(0.2f), Random(0.2f))
            };

            if (randomLights)
            {
                if (type == LightTypes.Point)
                {
                    info.PointLight.Range = Random(0.5f) * 2f;
                    info.PointLight.Attenuation = new(1, 1, 1);
                }
                else if (type == LightTypes.Spot)
                {
                    info.SpotLight.Range = Random(0.5f) * 2f;
                    info.SpotLight.Umbra = (Random(0.5f) - 0.4f) * MathF.PI;
                    info.SpotLight.Penumbra = info.SpotLight.Umbra + (0.1f * MathF.PI);
                    info.SpotLight.Attenuation = new(1, 1, 1);
                }
            }
            else
            {
                if (type == LightTypes.Point)
                {
                    info.PointLight.Range = 1f;
                    info.PointLight.Attenuation = new(1, 1, 1);
                }
                else if (type == LightTypes.Spot)
                {
                    info.SpotLight.Range = 2f;
                    info.SpotLight.Umbra = 0.7f * MathF.PI;
                    info.SpotLight.Penumbra = info.SpotLight.Umbra + (0.1f * MathF.PI);
                    info.SpotLight.Attenuation = new(1, 1, 1);
                }
            }

            var light = Application.CreateLight(info);
            Debug.Assert(light.IsValid);
            lights.Add(light);
        }

        public static void RemoveLights()
        {
            foreach (var light in lights)
            {
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                Application.RemoveEntity(id);
            }

            lights.Clear();
        }
    }
}
