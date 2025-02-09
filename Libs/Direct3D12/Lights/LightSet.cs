using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Utilities;

namespace Direct3D12.Lights
{
    class LightSet
    {
        // NOTE: these are NOT tightly packed
        private readonly FreeList<LightOwner> owners = new();
        private readonly List<Shaders.DirectionalLightParameters> nonCullableLights = [];
        private readonly List<uint> nonCullableOwners = [];

        // NOTE: these are tightly packed
        private readonly List<Shaders.LightParameters> cullableLights = [];
        private readonly List<Shaders.LightCullingLightInfo> cullingInfo = [];
        private readonly List<uint> cullableEntityIds = [];
        private readonly List<uint> cullableOwners = [];
        private readonly List<uint> dirtyBits = [];

        uint enabledLightCount = 0; // number of cullable lights

        public static class U32SetBits
        {
            public static uint Bits(int n)
            {
                if (n == 0) return 0;

                Debug.Assert(n > 0 && n < 32);
                return Bits(n - 1) | (1u << (n - 1));
            }
        }

        static readonly uint dirtybitsmask = U32SetBits.Bits(D3D12Graphics.FrameBufferCount);

        public LightSet()
        {
            Debug.Assert(U32SetBits.Bits(D3D12Graphics.FrameBufferCount) < (1u << 8), "That's quite a large frame buffer count!");
        }

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

                var parameters = nonCullableLights[(int)index];
                parameters.Color = info.Color;
                parameters.Intensity = info.Intensity;
                nonCullableLights[(int)index] = parameters;

                LightOwner owner = new()
                {
                    EntityId = info.EntityId,
                    DataIndex = index,
                    LightType = info.LightType,
                    IsEnabled = info.IsEnabled
                };
                uint id = owners.Add(owner);
                nonCullableOwners[(int)index] = id;

                return new(id, info.LightSetKey);
            }
            else
            {
                uint index = uint.MaxValue;

                // Try to find an empty slot
                for (uint i = enabledLightCount; i < cullableOwners.Count; i++)
                {
                    if (!IdDetail.IsValid(cullableOwners[(int)i]))
                    {
                        index = i;
                        break;
                    }
                }

                // If no empty slot was found then add a new item
                if (index == uint.MaxValue)
                {
                    index = (uint)cullableOwners.Count;
                    cullableLights.Add(default);
                    cullingInfo.Add(default);
                    cullableEntityIds.Add(default);
                    cullableOwners.Add(default);
                    dirtyBits.Add(default);
                    Debug.Assert(cullableOwners.Count == cullableLights.Count);
                    Debug.Assert(cullableOwners.Count == cullingInfo.Count);
                    Debug.Assert(cullableOwners.Count == cullableEntityIds.Count);
                    Debug.Assert(cullableOwners.Count == cullableOwners.Count);
                    Debug.Assert(cullableOwners.Count == dirtyBits.Count);
                }

                AddCullableLightParameters(info, index);
                AddLightCullingInfo(info, index);
                uint id = owners.Add(new() { EntityId = info.EntityId, DataIndex = index, LightType = info.LightType, IsEnabled = info.IsEnabled });
                cullableEntityIds[(int)index] = owners[id].EntityId;
                cullableOwners[(int)index] = id;
                dirtyBits[(int)index] = dirtybitsmask;
                Enable(id, info.IsEnabled);
                UpdateTransform(index);

                return new(id, info.LightSetKey);
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
                Debug.Assert(owners[cullableOwners[(int)owner.DataIndex]].DataIndex == owner.DataIndex);
                cullableOwners[(int)owner.DataIndex] = uint.MaxValue;
            }

            owners.Remove(id);
        }

        public void UpdateTransforms()
        {
            // Update direction for non-cullable lights
            foreach (uint id in nonCullableOwners)
            {
                if (!IdDetail.IsValid(id))
                {
                    continue;
                }

                LightOwner owner = owners[id];
                if (owner.IsEnabled)
                {
                    Entity entity = new(owner.EntityId);
                    var parameters = nonCullableLights[(int)owner.DataIndex];
                    parameters.Direction = entity.Orientation;
                    nonCullableLights[(int)owner.DataIndex] = parameters;
                }
            }

            // Update position and direction of cullable lights

            uint count = enabledLightCount;
            if (count == 0)
            {
                return;
            }

            Debug.Assert(cullableEntityIds.Count >= count);
            var transformFlagsCache = Transform.GetUpdatedComponentsFlags([.. cullableEntityIds]);

            for (uint i = 0; i < count; i++)
            {
                if (transformFlagsCache[(int)i] != 0)
                {
                    UpdateTransform(i);
                }
            }
        }

        public void Enable(uint id, bool isEnabled)
        {
            owners[id].IsEnabled = isEnabled;

            if (owners[id].LightType == LightTypes.Directional)
            {
                return;
            }

            // Cullable lights
            uint dataIndex = owners[id].DataIndex;

            // NOTE: this is a reference to _enabled_light_count and will change its value!
            // NOTE: ditry_bits is going to be set by swap_cullable_lightsm so we don't set it here.
            if (isEnabled)
            {
                if (dataIndex > enabledLightCount)
                {
                    Debug.Assert(enabledLightCount < cullableLights.Count);
                    SwapCullableLights(dataIndex, enabledLightCount);
                    enabledLightCount++;
                }
                else if (dataIndex == enabledLightCount)
                {
                    enabledLightCount++;
                }
            }
            else if (enabledLightCount > 0)
            {
                uint last = enabledLightCount - 1;
                if (dataIndex < last)
                {
                    SwapCullableLights(dataIndex, last);
                    enabledLightCount--;
                }
                else if (dataIndex == last)
                {
                    enabledLightCount--;
                }
            }
        }

        public void Intensity(uint id, float intensity)
        {
            if (intensity < 0f) intensity = 0f;

            LightOwner owner = owners[id];
            uint index = owner.DataIndex;

            if (owner.LightType == LightTypes.Directional)
            {
                Debug.Assert(index < nonCullableLights.Count);
                var light = nonCullableLights[(int)index];
                light.Intensity = intensity;
                nonCullableLights[(int)index] = light;
            }
            else
            {
                Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
                Debug.Assert(index < cullableLights.Count);
                var light = cullableLights[(int)index];
                light.Intensity = intensity;
                cullableLights[(int)index] = light;
                dirtyBits[(int)index] = dirtybitsmask;
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
                var light = nonCullableLights[(int)index];
                light.Color = color;
                nonCullableLights[(int)index] = light;
            }
            else
            {
                Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
                Debug.Assert(index < cullableLights.Count);
                var light = cullableLights[(int)index];
                light.Color = color;
                cullableLights[(int)index] = light;
                dirtyBits[(int)index] = dirtybitsmask;
            }
        }

        public void Attenuation(uint id, Vector3 attenuation)
        {
            Debug.Assert(attenuation.X >= 0f && attenuation.Y >= 0f && attenuation.Z >= 0f);
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            var light = cullableLights[(int)index];
            light.Attenuation = attenuation;
            cullableLights[(int)index] = light;
            dirtyBits[(int)index] = dirtybitsmask;
        }

        public void Range(uint id, float range)
        {
            Debug.Assert(range > 0f);
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            var light = cullableLights[(int)index];
            light.Range = range;
            cullableLights[(int)index] = light;
            dirtyBits[(int)index] = dirtybitsmask;
        }

        public void Umbra(uint id, float umbra)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);

            umbra = Math.Clamp(umbra, 0f, MathF.PI);

            var light = cullableLights[(int)index];
            light.CosUmbra = MathF.Cos(umbra * 0.5f);
            cullableLights[(int)index] = light;
            dirtyBits[(int)index] = dirtybitsmask;

            if (Penumbra(id) < umbra)
            {
                Penumbra(id, umbra);
            }
        }

        public void Penumbra(uint id, float penumbra)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);

            penumbra = Math.Clamp(penumbra, Umbra(id), MathF.PI);

            var light = cullableLights[(int)index];
            light.CosPenumbra = MathF.Cos(penumbra * 0.5f);
            cullableLights[(int)index] = light;

            var info = cullingInfo[(int)index];
            info.ConeRadius = CalculateConeRadius(Range(id), cullableLights[(int)index].CosPenumbra);
            cullingInfo[(int)index] = info;
            dirtyBits[(int)index] = dirtybitsmask;
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

            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(index < cullableLights.Count);
            return cullableLights[(int)index].Intensity;
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

            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(index < cullableLights.Count);
            return cullableLights[(int)index].Color;
        }

        public Vector3 Attenuation(uint id)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            return cullableLights[(int)index].Attenuation;
        }

        public float Range(uint id)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            return cullableLights[(int)index].Range;
        }

        public float Umbra(uint id)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            return MathF.Acos(cullableLights[(int)index].CosUmbra) * 2f;
        }

        public float Penumbra(uint id)
        {
            var owner = owners[id];
            uint index = owner.DataIndex;
            Debug.Assert(owners[cullableOwners[(int)index]].DataIndex == index);
            Debug.Assert(owner.LightType != LightTypes.Directional);
            Debug.Assert(index < cullableLights.Count);
            return MathF.Acos(cullableLights[(int)index].CosPenumbra) * 2f;
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
        public void NonCullableLights(out Shaders.DirectionalLightParameters[] lights)
        {
            lights = new Shaders.DirectionalLightParameters[nonCullableOwners.Count];

            uint index = 0;
            for (uint i = 0; i < lights.Length; i++)
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

        public uint CullableLightCount()
        {
            return enabledLightCount;
        }
        public void CullableLights(out Shaders.LightParameters[] lights)
        {
            lights = new Shaders.LightParameters[cullableOwners.Count];

            uint index = 0;
            for (uint i = 0; i < lights.Length; i++)
            {
                if (!IdDetail.IsValid(cullableOwners[(int)i]))
                {
                    continue;
                }

                LightOwner owner = owners[cullableOwners[(int)i]];
                if (owner.IsEnabled)
                {
                    Debug.Assert(owners[cullableOwners[(int)i]].DataIndex == i);
                    lights[index] = cullableLights[(int)i];
                    index++;
                }
            }
        }
        public Shaders.LightParameters CullableLights(uint index)
        {
            Debug.Assert(index < cullableOwners.Count);
            return cullableLights[(int)index];
        }
        public void CullingInfo(out Shaders.LightCullingLightInfo[] lights)
        {
            lights = new Shaders.LightCullingLightInfo[cullableOwners.Count];

            uint index = 0;
            for (uint i = 0; i < lights.Length; i++)
            {
                if (!IdDetail.IsValid(cullableOwners[(int)i]))
                {
                    continue;
                }

                LightOwner owner = owners[cullableOwners[(int)i]];
                if (owner.IsEnabled)
                {
                    Debug.Assert(owners[cullableOwners[(int)i]].DataIndex == i);
                    lights[index] = cullingInfo[(int)i];
                    index++;
                }
            }
        }
        public Shaders.LightCullingLightInfo CullingInfo(uint index)
        {
            Debug.Assert(index < cullingInfo.Count);
            return cullingInfo[(int)index];
        }

        public bool HasLights()
        {
            return owners.Size > 0;
        }


        private static float CalculateConeRadius(float range, float cosPenumbra)
        {
            float sinPenumbra = MathF.Sqrt(1f - cosPenumbra * cosPenumbra);

            if (cosPenumbra >= 0.707107f)
            {
                // if penumbra half angle is less than 45 degrees
                return sinPenumbra * range / cosPenumbra;
            }
            else
            {
                return sinPenumbra * range;
            }
        }

        private void UpdateTransform(uint index)
        {
            var entity = new Entity(cullableEntityIds[(int)index]);
            var parameters = cullableLights[(int)index];
            parameters.Position = entity.Position;

            var cInfo = cullingInfo[(int)index];
            cInfo.Position = parameters.Position;

            if (parameters.Type == (uint)LightTypes.Spot)
            {
                cInfo.Direction = parameters.Direction = entity.Orientation;
            }

            dirtyBits[(int)index] = dirtybitsmask;
        }

        private void AddCullableLightParameters(LightInitInfo info, uint index)
        {
            Debug.Assert(info.LightType != LightTypes.Directional && index < cullableLights.Count);

            var parameters = cullableLights[(int)index];
            parameters.Type = (uint)info.LightType;
            Debug.Assert(parameters.Type < (uint)LightTypes.Count);
            parameters.Color = info.Color;
            parameters.Intensity = info.Intensity;

            if (parameters.Type == (uint)LightTypes.Point)
            {
                var p = info.PointLight;
                parameters.Attenuation = p.Attenuation;
                parameters.Range = p.Range;
            }
            else if (parameters.Type == (uint)LightTypes.Spot)
            {
                var p = info.SpotLight;
                parameters.Attenuation = p.Attenuation;
                parameters.Range = p.Range;
                parameters.CosUmbra = MathF.Cos(p.Umbra * 0.5f);
                parameters.CosPenumbra = MathF.Cos(p.Penumbra * 0.5f);
            }
            cullableLights[(int)index] = parameters;
        }
        private void AddLightCullingInfo(LightInitInfo info, uint index)
        {
            Debug.Assert(info.LightType != LightTypes.Directional && index < cullingInfo.Count);

            var parameters = cullableLights[(int)index];
            Debug.Assert(parameters.Type == (uint)info.LightType);

            var cInfo = cullingInfo[(int)index];
            cInfo.Range = parameters.Range;
            cInfo.Type = parameters.Type;
            if (info.LightType == LightTypes.Spot)
            {
                cInfo.ConeRadius = CalculateConeRadius(parameters.Range, parameters.CosPenumbra);
            }
            cullingInfo[(int)index] = cInfo;
        }
        private void SwapCullableLights(uint index1, uint index2)
        {
            Debug.Assert(index1 != index2);

            //swap light parameter indices
            Debug.Assert(index1 < cullableOwners.Count);
            Debug.Assert(index2 < cullableOwners.Count);
            var owner1 = owners[cullableOwners[(int)index1]];
            var owner2 = owners[cullableOwners[(int)index2]];
            Debug.Assert(owner1.DataIndex == index1);
            Debug.Assert(owner2.DataIndex == index2);
            owner1.DataIndex = index2;
            owner2.DataIndex = index1;

            // swap light parameters
            Debug.Assert(index1 < cullableLights.Count);
            Debug.Assert(index2 < cullableLights.Count);
            (cullableLights[(int)index1], cullableLights[(int)index2]) = (cullableLights[(int)index2], cullableLights[(int)index1]);

            // swap culling info
            Debug.Assert(index1 < cullingInfo.Count);
            Debug.Assert(index2 < cullingInfo.Count);
            (cullingInfo[(int)index1], cullingInfo[(int)index2]) = (cullingInfo[(int)index2], cullingInfo[(int)index1]);

            // swap entity ids
            Debug.Assert(index1 < cullableEntityIds.Count);
            Debug.Assert(index2 < cullableEntityIds.Count);
            (cullableEntityIds[(int)index1], cullableEntityIds[(int)index2]) = (cullableEntityIds[(int)index2], cullableEntityIds[(int)index1]);

            // swap owner indices
            (cullableOwners[(int)index1], cullableOwners[(int)index2]) = (cullableOwners[(int)index2], cullableOwners[(int)index1]);

            Debug.Assert(owners[cullableOwners[(int)index1]].EntityId == cullableEntityIds[(int)index1]);
            Debug.Assert(owners[cullableOwners[(int)index2]].EntityId == cullableEntityIds[(int)index2]);

            // set dirty bits
            Debug.Assert(index1 < dirtyBits.Count);
            Debug.Assert(index2 < dirtyBits.Count);
            dirtyBits[(int)index1] = dirtybitsmask;
            dirtyBits[(int)index2] = dirtybitsmask;
        }


        public bool GetDirtyBit(uint index, uint value)
        {
            return (dirtyBits[(int)index] & value) != 0;
        }
        public void SetDirtyBit(uint index, uint value)
        {
            dirtyBits[(int)index] &= ~value;
        }
    }
}
