global using LightId = System.UInt32;
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
                return GetValue<bool>(id, LightParameters.IsEnabled);
            }
            set
            {
                SetValue(id, LightParameters.IsEnabled, value);
            }
        }
        public float Intensity
        {
            get
            {
                return GetValue<float>(id, LightParameters.Intensity);
            }
            set
            {
                SetValue(id, LightParameters.Intensity, value);
            }
        }
        public Vector3 Color
        {
            get
            {
                return GetValue<Vector3>(id, LightParameters.Color);
            }
            set
            {
                SetValue(id, LightParameters.Color, value);
            }
        }
        public LightTypes LightType
        {
            get
            {
                return (LightTypes)GetValue<uint>(id, LightParameters.LightType);
            }
        }
        public EntityId EntityId
        {
            get
            {
                return GetValue<EntityId>(id, LightParameters.EntityId);
            }
        }

        private T GetValue<T>(LightId id, LightParameters parameter) where T : unmanaged
        {
            Renderer.Gfx.GetParameter(id, lightSetKey, parameter, out T value);
            return value;
        }
        private void SetValue<T>(LightId id, LightParameters parameter, T value) where T : unmanaged
        {
            Renderer.Gfx.SetParameter(id, lightSetKey, parameter, value);
        }
    }
}
