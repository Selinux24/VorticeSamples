using System.Numerics;

namespace PrimalLike.EngineAPI
{
    public struct LightInitInfo()
    {
        public ulong LightSetKey = 0;
        public IdType EntityId = IdType.MaxValue;
        public LightTypes LightType = LightTypes.Directional;
        public float Intensity = 1.0f;
        public Vector3 Color = new(1.0f, 1.0f, 1.0f);
        public DirectionalLightParameters DirectionalLight;
        public PointLightParameters PointLight;
        public SpotLightParameters SpotLight;
        public AmbientParameters AmbientLight;
        public bool IsEnabled = true;
    }

    public struct DirectionalLightParameters
    {

    }

    public struct PointLightParameters
    {
        public Vector3 Attenuation;
        public float Range;
    }

    public struct SpotLightParameters
    {
        public Vector3 Attenuation;
        public float Range;
        public float Umbra;
        public float Penumbra;
    }

    public struct AmbientParameters()
    {
        public static uint TextureCount => 3;

        public IdType DiffuseTextureId = uint.MaxValue;
        public IdType SpecularTextureId = uint.MaxValue;
        public IdType BrdfLutTextureId = uint.MaxValue;
        
        public readonly IdType[] GetTextureIds()
        {
            return [DiffuseTextureId, SpecularTextureId, BrdfLutTextureId];
        }
    }
}

