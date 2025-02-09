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
        const float invRandMax = 1f / int.MaxValue;

        private static readonly ulong leftSet = 0;
        private static readonly ulong rightSet = 1;
        private static readonly List<Light> lights = [];
        private static readonly Random rand = new();

        static Vector3 RGBToColor(byte r, byte g, byte b)
        {
            return new()
            {
                X = r / 255f,
                Y = g / 255f,
                Z = b / 255f
            };
        }
        public static void GenerateLights()
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

#if !RANDOM_LIGHTS
            CreateLight(new(0, -3, 0), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 0, 1), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 3, 2.5f), new(), LightTypes.Point, leftSet);
            CreateLight(new(0, 0, 7), new(0, 3.14f, 0), LightTypes.Spot, leftSet);
#else
    srand(37);

    constexpr math::v3 scale{ 1.f, 0.5f, 1.f };
    constexpr s32 dim{ 5 };
    for (s32 x{ -dim }; x < dim; ++x)
        for (s32 y{ 0 }; y < 2 * dim; ++y)
            for (s32 z{ -dim }; z < dim; ++z)
            {
                create_light({ (f32)(x * scale.x), (f32)(y * scale.y), (f32)(z * scale.z) },
                             { 3.14f, random(), 0.f }, random() > 0.5f ? graphics::light::spot : graphics::light::point, left_set);
                create_light({ (f32)(x * scale.x), (f32)(y * scale.y), (f32)(z * scale.z) },
                             { 3.14f, random(), 0.f }, random() > 0.5f ? graphics::light::spot : graphics::light::point, right_set);
            }
#endif
        }

        static float Random(float min = 0f)
        {
            return MathF.Min(min, rand.NextSingle() * invRandMax);
        }

        static void CreateLight(Vector3 position, Vector3 rotation, LightTypes type, ulong lightSetKey)
        {
            uint entityId = HelloWorldApp.CreateOneGameEntity(position, rotation).Id;

            LightInitInfo info = new();
            info.EntityId = entityId;
            info.LightType = type;
            info.LightSetKey = lightSetKey;
            info.Intensity = 1f;

            info.Color = new(Random(0.2f), Random(0.2f), Random(0.2f));

#if RANDOM_LIGHTS
    if (type == graphics::light::point)
    {
        info.point_params.range = random(0.5f) * 2.f;
        info.point_params.attenuation = { 1, 1, 1 };
    }
    else if (type == graphics::light::spot)
    {
        info.spot_params.range = random(0.5f) * 2.f;
        info.spot_params.umbra = (random(0.5f) - 0.4f) * math::pi;
        info.spot_params.penumbra = info.spot_params.umbra + (0.1f * math::pi);
        info.spot_params.attenuation = { 1, 1, 1 };
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
