﻿using System;

namespace ShaderCompiler
{
    internal class Program
    {
        private const string shadersSourcePath = "../../../../../Libs/Direct3D12/Shaders/";
        private const string outputFileName = "engineShaders.bin";

        private const uint fullScreenTriangleVs = 0;
        private const uint fillColorPs = 1;
        private const uint postProcessPS = 2;

        private const uint shaderVertex = 0;
        private const uint shaderPixel = 4;

        private static readonly string[] profileStrings = ["vs_6_6", "hs_6_6", "ds_6_6", "gs_6_6", "ps_6_6", "cs_6_6", "as_6_6", "ms_6_6"];

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new (fullScreenTriangleVs, new ("FullScreenTriangle.hlsl", "FullScreenTriangleVS", shaderVertex, profileStrings[(int)shaderVertex])),
            new (fillColorPs, new ("FillColor.hlsl", "FillColorPS", shaderPixel, profileStrings[(int)shaderPixel])),
            new (postProcessPS, new ("PostProcess.hlsl", "PostProcessPS", shaderPixel, profileStrings[(int)shaderPixel])),
        ];

        static void Main()
        {
            bool res = ShaderCompilation.CompileShaders(shadersSourcePath, engineShaderFiles, outputFileName);

            Console.WriteLine(res ? "Shaders compiled successfully" : "Shaders compilation failed");
            Console.ReadKey();
        }
    }
}
