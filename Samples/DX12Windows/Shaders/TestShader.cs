using ContentTools;
using Direct3D12.ShaderCompiler;
using PrimalLike.Common;
using PrimalLike.Content;
using System.Diagnostics;
using System.IO;

namespace DX12Windows.Shaders
{
    static class TestShader
    {
        public enum TextureUsages : uint
        {
            AmbientOcclusion = 0,
            BaseColor,
            Emissive,
            MetalRough,
            Normal,

            Count
        }

        const string ShadersSourcePath = "./Hlsl/";
        const string ShadersIncludeDir = "../../../../../../Libs/Direct3D12/Hlsl/";

        public static uint VsId { get; private set; } = IdDetail.InvalidId;
        public static uint PsId { get; private set; } = IdDetail.InvalidId;
        public static uint TexPsId { get; private set; } = IdDetail.InvalidId;

        public static void Load()
        {
            Debug.Assert(File.Exists(Path.Combine(ShadersSourcePath, "TestShader.hlsl")));

            // Let's say our material uses a vertex shader and a pixel shader.
            {
                ShaderFileInfo info = new(Path.Combine(ShadersSourcePath, "TestShader.hlsl"), "MainVS", (uint)ShaderStage.Vertex);

                string[] defines = ["ELEMENTS_TYPE=0", "ELEMENTS_TYPE=1", "ELEMENTS_TYPE=3"];
                uint[] keys = [(uint)ElementsType.PositionOnly, (uint)ElementsType.StaticNormal, (uint)ElementsType.StaticNormalTexture];

                CompiledShader[] vertexShaders = new CompiledShader[defines.Length];
                for (uint i = 0; i < defines.Length; i++)
                {
                    string[] extraArgs = ["-D", defines[i],];
                    bool compiledVs = Compiler.Compile(info, ShadersIncludeDir, extraArgs, out var vertexShader);
                    Debug.Assert(compiledVs);
                    vertexShaders[i] = vertexShader;
                }
                VsId = ContentToEngine.AddShaderGroup(vertexShaders, keys);
            }

            {
                ShaderFileInfo info = new(Path.Combine(ShadersSourcePath, "TestShader.hlsl"), "MainPS", (uint)ShaderStage.Pixel);

                bool compiledPs = Compiler.Compile(info, ShadersIncludeDir, out var pixelShader);
                Debug.Assert(compiledPs);
                PsId = ContentToEngine.AddShaderGroup([pixelShader], [uint.MaxValue]);

                string[] extraArgs = ["-D", "TEXTURED_MTL=1"];
                bool compiledTexPs = Compiler.Compile(info, ShadersIncludeDir, extraArgs, out var texPixelShader);
                Debug.Assert(compiledTexPs);
                TexPsId = ContentToEngine.AddShaderGroup([texPixelShader], [uint.MaxValue]);
            }
        }
        public static void Remove()
        {
            // remove shaders
            if (IdDetail.IsValid(VsId)) ContentToEngine.RemoveShaderGroup(VsId);

            if (IdDetail.IsValid(PsId)) ContentToEngine.RemoveShaderGroup(PsId);

            if (IdDetail.IsValid(TexPsId)) ContentToEngine.RemoveShaderGroup(TexPsId);
        }
    }
}
