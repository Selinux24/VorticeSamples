using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Utilities;

namespace Direct3D12.Lights
{
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

            // TODO: cullable lights
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
                var light = nonCullableLights[(int)index];
                light.Intensity = intensity;
                nonCullableLights[(int)index] = light;
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
                var light = nonCullableLights[(int)index];
                light.Color = color;
                nonCullableLights[(int)index] = light;
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

        public bool HasLights()
        {
            return owners.Size > 0;
        }
    }
}
