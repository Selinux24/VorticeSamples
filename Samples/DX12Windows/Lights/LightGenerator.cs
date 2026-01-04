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
        const int randMax = 0x7fff;
        const float invRandMax = 1f / randMax;

        static readonly ulong leftSet = 0;
        static readonly ulong rightSet = 1;
        static readonly List<Light> lights = [];
        static readonly List<Light> disabledLights = [];
        static readonly Random rand = new(37);

        static Light iblLight = new();

#if ANIMATE_LIGHTS
        static float t = 0;
#endif

        static Vector3 RGBToColor(byte r, byte g, byte b)
        {
            return new()
            {
                X = r / 255f,
                Y = g / 255f,
                Z = b / 255f
            };
        }
        static float Random(float min = 0f)
        {
            float v = rand.Next(0, randMax) * invRandMax;

            return MathF.Max(min, v);
        }

        public static void GenerateLights(int lightSet)
        {
            if (lightSet == 1)
            {
                GenerateLightsForLab();
            }
            else
            {
                GenerateLightsDefault();
            }
        }
        static void GenerateLightsForLab()
        {
            Application.CreateLightSet(leftSet);
            Application.CreateLightSet(rightSet);

            // LEFT_SET
            LightInitInfo info = new()
            {
                EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity).Id,
                LightType = LightTypes.Directional,
                LightSetKey = leftSet,
                Intensity = 1f,
                Color = RGBToColor(174, 174, 174)
            };
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, -MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));

            // RIGHT_SET
            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity).Id;
            info.LightSetKey = rightSet;
            info.Color = RGBToColor(150, 100, 200);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, -MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(163, 47, 30);
            lights.Add(Application.CreateLight(info));

#if RANDOM_LIGHTS
            float scale1 = 2f;
            Vector3 scale = new(1f * scale1, 0.5f * scale1, 1f * scale1);
            int dim = 10;
            for (int x = -dim; x < dim; x++)
            {
                for (int y = 0; y < 2 * dim; y++)
                {
                    for (int z = -dim; z < dim; z++)
                    {
                        CreateLight(
                            new(x * scale.X, y * scale.Y, z * scale.Z),
                            new(Random() * 3.14f, Random() * 3.14f, Random() * 3.14f),
                            Random() > 0.5f ? LightTypes.Spot : LightTypes.Point,
                            leftSet);
                        CreateLight(
                            new(x * scale.X, y * scale.Y, z * scale.Z),
                            new(Random() * 3.14f, Random() * 3.14f, Random() * 3.14f),
                            Random() > 0.5f ? LightTypes.Spot : LightTypes.Point,
                            rightSet);
                    }
                }
            }
#else
            CreateLight(new(0, -3, 0), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 0.2f, 1f), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 3, 2.5f), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 0.1f, 7), new(0, 3.14f, 0), LightTypes.Spot, leftSet);
#endif
        }
        static void GenerateLightsDefault()
        {
            Application.CreateLightSet(leftSet);
            Application.CreateLightSet(rightSet);

            // LEFT_SET
            LightInitInfo info = new()
            {
                EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity).Id,
                LightType = LightTypes.Directional,
                LightSetKey = leftSet,
                Intensity = 1f,
                Color = RGBToColor(174, 174, 174)
            };
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, -MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));

            // RIGHT_SET
            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.Identity).Id;
            info.LightSetKey = rightSet;
            info.Color = RGBToColor(150, 100, 200);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(0f, -MathHelper.PiOver2, 0f)).Id;
            info.Color = RGBToColor(163, 47, 30);
            lights.Add(Application.CreateLight(info));
        }
        static void CreateLight(Vector3 position, Vector3 rotation, LightTypes type, ulong lightSetKey)
        {
#if ROTATE_LIGHTS
            uint entityId = type == LightTypes.Spot ?
                HelloWorldApp.CreateOneGameEntity<Scripts.RotatorScript>(position, rotation).Id :
                HelloWorldApp.CreateOneGameEntity(position, rotation).Id;
#else
            uint entityId = HelloWorldApp.CreateOneGameEntity(position, Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z)).Id;
#endif

            LightInitInfo info = new()
            {
                EntityId = entityId,
                LightType = type,
                LightSetKey = lightSetKey,
                Intensity = 1f,

                Color = new(Random(0.2f), Random(0.2f), Random(0.2f))
            };

#if RANDOM_LIGHTS
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
#else
            if (type == LightTypes.Point)
            {
                info.PointLight.Range = 1f;
                info.PointLight.Attenuation = new(1, 1, 1);
            }
            else if (type == LightTypes.Spot)
            {
                info.SpotLight.Range = 2f;
                info.SpotLight.Umbra = 0.1f * MathF.PI;
                info.SpotLight.Penumbra = info.SpotLight.Umbra + (0.1f * MathF.PI);
                info.SpotLight.Attenuation = new(1, 1, 1);
            }
#endif

            var light = Application.CreateLight(info);
            Debug.Assert(light.IsValid);
            lights.Add(light);
        }

        public static void CreateIblLight(uint iblBrdfLutId, uint iblDiffuseId, uint iblSpecularId)
        {
            LightInitInfo info = new()
            {
                EntityId = 0,
                LightType = LightTypes.Ambient,
                LightSetKey = leftSet,
            };
            info.AmbientLight.BrdfLutTextureId = iblBrdfLutId;
            info.AmbientLight.DiffuseTextureId = iblDiffuseId;
            info.AmbientLight.SpecularTextureId = iblSpecularId;

            iblLight = Application.CreateLight(info);
        }

        public static void RemoveLights()
        {
            if (iblLight.IsValid)
            {
                Application.RemoveLight(iblLight.Id, iblLight.LightSetKey);
            }

            foreach (var light in lights)
            {
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                Application.RemoveEntity(id);
            }
            lights.Clear();

            foreach (var light in disabledLights)
            {
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                Application.RemoveEntity(id);
            }
            disabledLights.Clear();

            Application.RemoveLightSet(leftSet);
            Application.RemoveLightSet(rightSet);
        }

        public static void TestLights(float dt)
        {
#if ANIMATE_LIGHTS
            t += 0.05f;
            for (int i = 0; i < lights.Count; i++)
            {
                float sine = MathF.Sin(t + lights[i].Id);
                sine *= sine;
                lights[i].Intensity = 2f * sine;
            }
#else
            uint count = (uint)(Random(0.1f) * 100);
            for (int i = 0; i < count; i++)
            {
                if (lights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (lights.Count - 1));
                Light light = lights[index];
                light.IsEnabled = false;
                lights.RemoveAt(index);
                disabledLights.Add(light);
            }

            count = (uint)(Random(0.1f) * 50);
            for (int i = 0; i < count; i++)
            {
                if (lights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (lights.Count - 1));
                Light light = lights[index];
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                Application.RemoveEntity(id);
                lights.RemoveAt(index);
            }

            count = (uint)(Random(0.1f) * 50);
            for (int i = 0; i < count; i++)
            {
                if (disabledLights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (disabledLights.Count - 1));
                Light light = disabledLights[index];
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                Application.RemoveEntity(id);
                disabledLights.RemoveAt(index);
            }

            count = (uint)(Random(0.1f) * 100);
            for (int i = 0; i < count; i++)
            {
                if (disabledLights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (disabledLights.Count - 1));
                Light light = disabledLights[index];
                light.IsEnabled = true;
                disabledLights.RemoveAt(index);
                lights.Add(light);
            }

            float scale1 = 1;
            Vector3 scale = new(1f * scale1, 0.5f * scale1, 1f * scale1);
            count = (uint)(Random(0.1f) * 50);

            for (int i = 0; i < count; i++)
            {
                Vector3 p1 = new((Random() * 2 - 1f) * 13f * scale.X, Random() * 2 * 13f * scale.Y, (Random() * 2 - 1f) * 13f * scale.Z);
                Vector3 p2 = new((Random() * 2 - 1f) * 13f * scale.X, Random() * 2 * 13f * scale.Y, (Random() * 2 - 1f) * 13f * scale.Z);
                CreateLight(
                    p1,
                    new Vector3(Random() * 3.14f, Random() * 3.14f, Random() * 3.14f),
                    Random() > 0.5f ? LightTypes.Spot : LightTypes.Point, leftSet);
                CreateLight(
                    p2,
                    new Vector3(Random() * 3.14f, Random() * 3.14f, Random() * 3.14f),
                    Random() > 0.5f ? LightTypes.Spot : LightTypes.Point, rightSet);
            }
#endif
        }
    }
}
