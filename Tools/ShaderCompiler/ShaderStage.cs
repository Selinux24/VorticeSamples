using Vortice.Dxc;

namespace ShaderCompiler
{
    public enum ShaderStage
    {
        Vertex = (int)DxcShaderStage.Vertex,
        Hull = (int)DxcShaderStage.Hull,
        Domain = (int)DxcShaderStage.Domain,
        Geometry = (int)DxcShaderStage.Geometry,
        Pixel = (int)DxcShaderStage.Pixel,
        Compute = (int)DxcShaderStage.Compute,
        Amplification = (int)DxcShaderStage.Amplification,
        Mesh = (int)DxcShaderStage.Mesh,
        Library = (int)DxcShaderStage.Library,
        Count = (int)DxcShaderStage.Count
    }
}
