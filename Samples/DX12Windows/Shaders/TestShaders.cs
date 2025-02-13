using ContentTools;
using PrimalLike.Common;
using PrimalLike.Content;
using ShaderCompiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DX12Windows.Shaders
{
    static class TestShaders
    {
        private const string shadersSourcePath = "./Shaders/";
        private const string shadersIncludeDir = "../../../../../Libs/Direct3D12/Shaders/";

        public static uint VsId { get; private set; } = IdDetail.InvalidId;
        public static uint PsId { get; private set; } = IdDetail.InvalidId;

        public static void LoadShaders()
        {
            // Let's say our material uses a vertex shader and a pixel shader.
            ShaderFileInfo info = new(Path.Combine(shadersSourcePath, "TestShader.hlsl"), "TestShaderVS", ShaderStage.Vertex);

            string[] defines = ["ELEMENTS_TYPE=1", "ELEMENTS_TYPE=3"];
            uint[] keys =
            [
                (uint)ElementsType.StaticNormal,
                (uint)ElementsType.StaticNormalTexture,
            ];

            List<string> extraArgs = [];
            PrimalLike.Content.CompiledShader[] vertexShaders = new PrimalLike.Content.CompiledShader[2];
            for (uint i = 0; i < defines.Length; i++)
            {
                extraArgs.Clear();
                extraArgs.Add("-D");
                extraArgs.Add(defines[i]);
                bool compiledVs = Compiler.Compile(info, shadersIncludeDir, extraArgs, out var vertexShader);
                Debug.Assert(compiledVs);
                vertexShaders[i] = new(vertexShader.ByteCode, vertexShader.Hash.HashDigest);
            }

            VsId = ContentToEngine.AddShaderGroup(vertexShaders, keys);

            info = new ShaderFileInfo(Path.Combine(shadersSourcePath, "TestShader.hlsl"), "TestShaderPS", ShaderStage.Pixel);
            bool compiledPs = Compiler.Compile(info, shadersIncludeDir, out var pixelShader);
            Debug.Assert(compiledPs);

            PrimalLike.Content.CompiledShader[] pixelShaders =
            [
                new PrimalLike.Content.CompiledShader(pixelShader.ByteCode, pixelShader.Hash.HashDigest)
            ];

            PsId = ContentToEngine.AddShaderGroup(pixelShaders, [uint.MaxValue]);
        }
        public static void RemoveShaders()
        {
            // remove shaders and textures
            if (IdDetail.IsValid(VsId))
            {
                ContentToEngine.RemoveShaderGroup(VsId);
            }

            if (IdDetail.IsValid(PsId))
            {
                ContentToEngine.RemoveShaderGroup(PsId);
            }
        }
    }
}
