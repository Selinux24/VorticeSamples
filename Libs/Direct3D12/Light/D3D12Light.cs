using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Direct3D12.Light
{
    public static class D3D12Light
    {
        static readonly Dictionary<ulong, LightSet> lightSets = [];
        static readonly D3D12LightBuffer[] lightBuffers = new D3D12LightBuffer[D3D12Graphics.FrameBufferCount];

        delegate void SetFunction(LightSet set, uint id, object value);
        delegate object GetFunction(LightSet set, uint id);

        static readonly SetFunction[] setFunctions =
        [
            (set, id, value)=>SetIsEnabled(set, id, (bool)value),
            (set, id, value)=>SetIntensity(set, id, (float)value),
            (set, id, value)=>SetColor(set, id, (Vector3)value),
            (set, id, value)=>SetAttenuation(set, id, (Vector3)value),
            (set, id, value)=>SetRange(set, id, (float)value),
            (set, id, value)=>SetUmbra(set, id, (float)value),
            (set, id, value)=>SetPenumbra(set, id, (float)value),
            DummySet,
            DummySet,
        ];
        static readonly GetFunction[] getFunctions =
        [
            (set, id)=>GetIsEnabled(set, id),
            (set, id)=>GetIntensity(set, id),
            (set, id)=>GetColor(set, id),
            (set, id)=>GetAttenuation(set, id),
            (set, id)=>GetRange(set, id),
            (set, id)=>GetUmbra(set, id),
            (set, id)=>GetPenumbra(set, id),
            (set, id)=>GetLightType(set, id),
            (set, id)=>GetEntityId(set, id),
        ];

        public static bool Initialize()
        {
            for (int i = 0; i < lightBuffers.Length; i++)
            {
                lightBuffers[i] = new();
            }

            return true;
        }

        public static void Shutdown()
        {
            // make sure to remove all lights before shutting down graphics.
            Debug.Assert(lightSets.Count == 0);

            for (uint i = 0; i < D3D12Graphics.FrameBufferCount; i++)
            {
                lightBuffers[i].Dispose();
            }
        }

        public static void CreateLightSet(ulong lightSetKey)
        {
            Debug.Assert(!lightSets.ContainsKey(lightSetKey));
            lightSets.Add(lightSetKey, new());
        }
        public static void RemoveLightSet(ulong lightSetKey)
        {
            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            Debug.Assert(lightSets[lightSetKey].HasLights() == false);
            lightSets.Remove(lightSetKey);
        }

        public static PrimalLike.EngineAPI.Light Create(LightInitInfo info)
        {
            Debug.Assert(IdDetail.IsValid(info.EntityId));
            Debug.Assert(lightSets.ContainsKey(info.LightSetKey));
            if (lightSets.TryGetValue(info.LightSetKey, out var value))
            {
                return value.Add(info);
            }
            else
            {
                LightSet set = new();
                lightSets.Add(info.LightSetKey, set);
                return set.Add(info);
            }
        }
        public static void Remove(uint id, ulong lightSetKey)
        {
            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            lightSets[lightSetKey].Remove(id);
        }
        public static void SetParameter<T>(uint id, ulong lightSetKey, LightParametersTypes parameter, T value) where T : unmanaged
        {
            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            Debug.Assert((uint)parameter < (uint)LightParametersTypes.Count);
            Debug.Assert(setFunctions[(uint)parameter] != DummySet);
            setFunctions[(uint)parameter](lightSets[lightSetKey], id, value);
        }
        public static void GetParameter<T>(uint id, ulong lightSetKey, LightParametersTypes parameter, out T value) where T : unmanaged
        {
            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            Debug.Assert((uint)parameter < (uint)LightParametersTypes.Count);
            value = (T)getFunctions[(int)parameter](lightSets[lightSetKey], id);
        }

        static void SetIsEnabled(LightSet set, uint id, bool value)
        {
            set.Enable(id, value);
        }
        static void SetIntensity(LightSet set, uint id, float value)
        {
            set.Intensity(id, value);
        }
        static void SetColor(LightSet set, uint id, Vector3 value)
        {
            set.Color(id, value);
        }
        static void SetAttenuation(LightSet set, uint id, Vector3 value)
        {
            set.Attenuation(id, value);
        }
        static void SetRange(LightSet set, uint id, float value)
        {
            set.Range(id, value);
        }
        static void SetUmbra(LightSet set, uint id, float value)
        {
            set.Umbra(id, value);
        }
        static void SetPenumbra(LightSet set, uint id, float value)
        {
            set.Penumbra(id, value);
        }
        static void DummySet(LightSet set, uint id, object value)
        {

        }

        static bool GetIsEnabled(LightSet set, uint id)
        {
            return set.IsEnabled(id);
        }
        static float GetIntensity(LightSet set, uint id)
        {
            return set.Intensity(id);
        }
        static Vector3 GetColor(LightSet set, uint id)
        {
            return set.Color(id);
        }
        static Vector3 GetAttenuation(LightSet set, uint id)
        {
            return set.Attenuation(id);
        }
        static float GetRange(LightSet set, uint id)
        {
            return set.Range(id);
        }
        static float GetUmbra(LightSet set, uint id)
        {
            return set.Umbra(id);
        }
        static float GetPenumbra(LightSet set, uint id)
        {
            return set.Penumbra(id);
        }
        static LightTypes GetLightType(LightSet set, uint id)
        {
            return set.LightType(id);
        }
        static uint GetEntityId(LightSet set, uint id)
        {
            return set.EntityId(id);
        }

        public static void UpdateLightBuffers(ref D3D12FrameInfo d3d12Info)
        {
            ulong lightSetKey = d3d12Info.FrameInfo.LightSetKey;
            if (lightSetKey == ulong.MaxValue)
            {
                return;
            }

            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            var set = lightSets[lightSetKey];
            if (!set.HasLights())
            {
                return;
            }

            set.UpdateTransforms();

            uint frameIndex = d3d12Info.FrameIndex;
            lightBuffers[frameIndex].UpdateLightBuffers(set, lightSetKey, frameIndex);
        }
        public static ulong NonCullableLightBuffer(uint frameIndex)
        {
            return lightBuffers[frameIndex].NonCullableLights();
        }
        public static ulong CullableLightBuffer(uint frameIndex)
        {
            return lightBuffers[frameIndex].CullableLights();
        }
        public static ulong CullingInfoBuffer(uint frameIndex)
        {
            return lightBuffers[frameIndex].CullingInfo();
        }
        public static ulong BoundingSpheresBuffer(uint frameIndex)
        {
            return lightBuffers[frameIndex].BoundingSpheres();
        }
        public static Shaders.AmbientLightParameters AmbientLight(ulong lightSetKey)
        {
            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            return lightSets[lightSetKey].AmbientLight();
        }
        public static uint NonCullableLightCount(ulong lightSetKey)
        {
            if (lightSetKey == ulong.MaxValue)
            {
                return 0;
            }

            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            return lightSets[lightSetKey].NonCullableLightCount();
        }
        public static uint CullableLightCount(ulong lightSetKey)
        {
            if (lightSetKey == ulong.MaxValue)
            {
                return 0;
            }

            Debug.Assert(lightSets.ContainsKey(lightSetKey));
            return lightSets[lightSetKey].CullableLightCount();
        }
    }
}
