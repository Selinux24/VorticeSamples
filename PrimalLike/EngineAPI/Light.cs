﻿global using LightId = System.UInt32;
using PrimalLike.Common;
using PrimalLike.Graphics;
using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public class Light
    {
        private readonly LightId id;
        private readonly ulong lightSetKey;

        public Light()
        {
            id = LightId.MaxValue;
        }
        public Light(LightId id, ulong lightSetKey)
        {
            this.id = id;
            this.lightSetKey = lightSetKey;
        }

        public LightId Id { get => id; }
        public ulong LightSetKey { get => lightSetKey; }
        public bool IsValid { get => IdDetail.IsValid(id); }

        public bool IsEnabled
        {
            get
            {
                return GetValue<bool>(id, LightParametersTypes.IsEnabled);
            }
            set
            {
                SetValue(id, LightParametersTypes.IsEnabled, value);
            }
        }
        public float Intensity
        {
            get
            {
                return GetValue<float>(id, LightParametersTypes.Intensity);
            }
            set
            {
                SetValue(id, LightParametersTypes.Intensity, value);
            }
        }
        public Vector3 Color
        {
            get
            {
                return GetValue<Vector3>(id, LightParametersTypes.Color);
            }
            set
            {
                SetValue(id, LightParametersTypes.Color, value);
            }
        }
        public Vector3 Attenuation
        {
            get
            {
                return GetValue<Vector3>(id, LightParametersTypes.Attenuation);
            }
            set
            {
                SetValue(id, LightParametersTypes.Attenuation, value);
            }
        }
        public float Range
        {
            get
            {
                return GetValue<float>(id, LightParametersTypes.Range);
            }
            set
            {
                SetValue(id, LightParametersTypes.Range, value);
            }
        }
        public float Umbra
        {
            get
            {
                return GetValue<float>(id, LightParametersTypes.Umbra);
            }
            set
            {
                SetValue(id, LightParametersTypes.Umbra, value);
            }
        }
        public float Penumbra
        {
            get
            {
                return GetValue<float>(id, LightParametersTypes.Penumbra);
            }
            set
            {
                SetValue(id, LightParametersTypes.Penumbra, value);
            }
        }
        public LightTypes LightType
        {
            get
            {
                return (LightTypes)GetValue<uint>(id, LightParametersTypes.LightType);
            }
        }
        public EntityId EntityId
        {
            get
            {
                return GetValue<EntityId>(id, LightParametersTypes.EntityId);
            }
        }

        private T GetValue<T>(LightId id, LightParametersTypes parameter) where T : unmanaged
        {
            Renderer.Gfx.GetParameter(id, lightSetKey, parameter, out T value);
            return value;
        }
        private void SetValue<T>(LightId id, LightParametersTypes parameter, T value) where T : unmanaged
        {
            Renderer.Gfx.SetParameter(id, lightSetKey, parameter, value);
        }
    }
}
