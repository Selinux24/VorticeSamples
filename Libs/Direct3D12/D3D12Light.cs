using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;

namespace Direct3D12
{
    public static class D3D12Light
    {
        private static readonly Dictionary<ulong, LightSet> lightSets = [];

        delegate void SetFunction(LightSet set, uint id, object value);
        delegate object GetFunction(LightSet set, uint id);
        private static readonly SetFunction[] setFunctions =
        [
            (set, id, value)=>SetIsEnabled(set, id, (bool)value),
            (set, id, value)=>SetIntensity(set, id, (float)value),
            (set, id, value)=>SetColor(set, id, (Vector3)value),
            DummySet,
            DummySet,
        ];
        private static readonly GetFunction[] getFunctions =
        [
            (set, id)=>GetIsEnabled(set, id),
            (set, id)=>GetIntensity(set, id),
            (set, id)=>GetColor(set, id),
            (set, id)=>GetLightType(set, id),
            (set, id)=>GetEntityId(set, id),
        ];

        record LightOwner
        {
            public uint EntityId;
            public uint DataIndex;
            public LightTypes LightType;
            public bool IsEnabled;
        }

        class LightSet
        {
            private readonly FreeList<LightOwner> owners = new();
            private readonly List<Shaders.DirectionalLightParameters> nonCullableLights = [];
            private readonly List<uint> nonCullableOwners = [];

            public Light Add(LightInitInfo info)
            {
                if (info.LightType == LightTypes.Directional)
                {
                    uint index = uint.MaxValue;
                    // Find an available slot in the array if any.
                    for (uint i = 0; i < nonCullableOwners.Count; i++)
                    {
                        if (!IdDetail.IsValid(nonCullableOwners[(int)i]))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == uint.MaxValue)
                    {
                        index = (uint)nonCullableOwners.Count;
                        nonCullableOwners.Add(default);
                        nonCullableLights.Add(new());
                    }

                    Shaders.DirectionalLightParameters parameters = nonCullableLights[(int)index];
                    parameters.Color = info.Color;
                    parameters.Intensity = info.Intensity;

                    LightOwner owner = new()
                    {
                        EntityId = info.EntityId,
                        DataIndex = index,
                        LightType = info.LightType,
                        IsEnabled = info.IsEnabled
                    };
                    uint id = owners.Add(owner);
                    nonCullableOwners[(int)index] = id;

                    return new Light(id, info.LightSetKey);
                }
                else
                {
                    return default;
                }
            }

            public void Remove(uint id)
            {
                Enable(id, false);

                LightOwner owner = owners[id];

                if (owner.LightType == LightTypes.Directional)
                {
                    nonCullableOwners[(int)owner.DataIndex] = uint.MaxValue;
                }
                else
                {
                    // TODO: cullable lights
                }

                owners.Remove(id);
            }

            public void Enable(uint id, bool isEnabled)
            {
                owners[id].IsEnabled = isEnabled;

                if (owners[id].LightType == LightTypes.Directional)
                {
                    return;
                }

                // TODO: cullable lights
            }

            public void Intensity(uint id, float intensity)
            {
                if (intensity < 0f) intensity = 0f;

                LightOwner owner = owners[id];
                uint index = owner.DataIndex;

                if (owner.LightType == LightTypes.Directional)
                {
                    Debug.Assert(index < nonCullableLights.Count);
                    nonCullableLights[(int)index].Intensity = intensity;
                }
                else
                {
                    // TODO: cullable lights
                }
            }

            public void Color(uint id, Vector3 color)
            {
                Debug.Assert(color.X <= 1f && color.Y <= 1f && color.Z <= 1f);
                Debug.Assert(color.X >= 0f && color.Y >= 0f && color.Z >= 0f);

                LightOwner owner = owners[id];
                uint index = owner.DataIndex;

                if (owner.LightType == LightTypes.Directional)
                {
                    Debug.Assert(index < nonCullableLights.Count);
                    nonCullableLights[(int)index].Color = color;
                }
                else
                {
                    // TODO: cullable lights
                }
            }

            public bool IsEnabled(uint id)
            {
                return owners[id].IsEnabled;
            }

            public float Intensity(uint id)
            {
                LightOwner owner = owners[id];
                uint index = owner.DataIndex;
                if (owner.LightType == LightTypes.Directional)
                {
                    Debug.Assert(index < nonCullableLights.Count);
                    return nonCullableLights[(int)index].Intensity;
                }

                // TODO: cullable lights
                return 0f;
            }

            public Vector3 Color(uint id)
            {
                LightOwner owner = owners[id];
                uint index = owner.DataIndex;
                if (owner.LightType == LightTypes.Directional)
                {
                    Debug.Assert(index < nonCullableLights.Count);
                    return nonCullableLights[(int)index].Color;
                }

                // TODO: cullable lights
                return default;
            }

            public LightTypes LightType(uint id)
            {
                return owners[id].LightType;
            }

            public uint EntityId(uint id)
            {
                return owners[id].EntityId;
            }

            // Return the number of enabled directional lights
            public uint NonCullableLightCount()
            {
                uint count = 0;
                foreach (var id in nonCullableOwners)
                {
                    if (IdDetail.IsValid(id) && owners[id].IsEnabled)
                    {
                        count++;
                    }
                }

                return count;
            }

            public void NonCullableLights(Shaders.DirectionalLightParameters[] lights)
            {
                uint count = (uint)nonCullableOwners.Count;
                ulong size = (ulong)Marshal.SizeOf<Shaders.DirectionalLightParameters>();
                Debug.Assert(size == D3D12Helpers.AlignSizeForConstantBuffer(size * count));

                uint index = 0;
                for (uint i = 0; i < count; i++)
                {
                    if (!IdDetail.IsValid(nonCullableOwners[(int)i]))
                    {
                        continue;
                    }

                    LightOwner owner = owners[nonCullableOwners[(int)i]];
                    if (owner.IsEnabled)
                    {
                        Debug.Assert(owners[nonCullableOwners[(int)i]].DataIndex == i);
                        lights[index] = nonCullableLights[(int)i];
                        index++;
                    }
                }
            }
        }

        public static Light Create(LightInitInfo info)
        {
            Debug.Assert(IdDetail.IsValid(info.EntityId));
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
            lightSets[lightSetKey].Remove(id);
        }
        public static void SetParameter<T>(uint id, ulong lightSetKey, LightParameters parameter, T value) where T : unmanaged
        {
            Debug.Assert((uint)parameter < (uint)LightParameters.Count);
            Debug.Assert(setFunctions[(uint)parameter] != DummySet);
            setFunctions[(uint)parameter](lightSets[lightSetKey], id, value);
        }
        public static void GetParameter<T>(uint id, ulong lightSetKey, LightParameters parameter, out T value) where T : unmanaged
        {
            Debug.Assert((uint)parameter < (uint)LightParameters.Count);
            value = (T)getFunctions[(int)parameter](lightSets[lightSetKey], id);
        }

        private static void SetIsEnabled(LightSet set, uint id, bool value)
        {
            set.Enable(id, value);
        }
        private static void SetIntensity(LightSet set, uint id, float value)
        {
            set.Intensity(id, value);
        }
        private static void SetColor(LightSet set, uint id, Vector3 value)
        {
            set.Color(id, value);
        }
        private static void DummySet(LightSet set, uint id, object value)
        {

        }

        private static bool GetIsEnabled(LightSet set, uint id)
        {
            return set.IsEnabled(id);
        }
        private static float GetIntensity(LightSet set, uint id)
        {
            return set.Intensity(id);
        }
        private static Vector3 GetColor(LightSet set, uint id)
        {
            return set.Color(id);
        }
        private static LightTypes GetLightType(LightSet set, uint id)
        {
            return set.LightType(id);
        }
        private static uint GetEntityId(LightSet set, uint id)
        {
            return set.EntityId(id);
        }
    }
}
