﻿using Direct3D12;
using ShaderCompiler;
using System;
using System.IO;

namespace DX12Windows.Shaders
{
    static class EngineShadersHelper
    {
        private const string shadersSourceDir = "../../../../../Libs/Direct3D12/Hlsl/";
        private const string shadersIncludeDir = "../../../../../Libs/Direct3D12/Hlsl/";
        private const string shadersOutputPath = "./Content/engineShaders.bin";

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new (Path.Combine(shadersSourceDir, "FullScreenTriangle.hlsl"), "FullScreenTriangleVS", ShaderStage.Vertex)),
            new ((int)EngineShaders.FillColorPs, new (Path.Combine(shadersSourceDir, "FillColor.hlsl"), "FillColorPS", ShaderStage.Pixel)),
            new ((int)EngineShaders.PostProcessPs, new (Path.Combine(shadersSourceDir, "PostProcess.hlsl"), "PostProcessPS", ShaderStage.Pixel)),
            new ((int)EngineShaders.GridFrustumsCs, new (Path.Combine(shadersSourceDir, "GridFrustums.hlsl"), "ComputeGridFrustumsCS", ShaderStage.Compute), ["-D", "TILE_SIZE=32"]),
            new ((int)EngineShaders.LightCullingCs, new (Path.Combine(shadersSourceDir, "CullLights.hlsl"), "CullLightsCS", ShaderStage.Compute), ["-D", "TILE_SIZE=32"]),
        ];

        public static void Compile()
        {
            if (!Compiler.CompileShaders(engineShaderFiles, shadersIncludeDir, shadersOutputPath))
            {
                Console.WriteLine("Engine shaders compilation failed");
            }
        }
    }
}
