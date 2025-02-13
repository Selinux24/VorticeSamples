
namespace Direct3D12
{
    public enum OpaqueRootParameter : uint
    {
        GlobalShaderData,
        PerObjectData,
        PositionBuffer,
        ElementBuffer,
        SrvIndices,
        DirectionalLights,
        CullableLights,
        LightGrid,
        LightIndexList,

        Count
    }
}
