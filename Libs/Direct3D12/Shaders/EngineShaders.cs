
namespace Direct3D12.Shaders
{
    /// <summary>
    /// Built-in engine shaders.
    /// </summary>
    public enum EngineShaders : uint
    {
        FullScreenTriangleVs = 0,
        FillColorPs,
        PostProcessPs,
        GridFrustumsCs,
        LightCullingCs,

        Count,
    }
}
