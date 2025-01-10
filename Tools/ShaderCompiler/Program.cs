using System;

namespace ShaderCompiler
{
    internal class Program
    {
        private const string shadersSourcePath = "../../../../../Libs/Direct3D12/Shaders/";
        private const string outputFileName = "./OutputShaders/engineShaders.bin";

        private const uint fullScreenTriangleVs = 0;
        private const uint fillColorPs = 1;
        private const uint postProcessPS = 2;

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new (fullScreenTriangleVs, new ("FullScreenTriangle.hlsl", "FullScreenTriangleVS", ShaderStage.Vertex)),
            new (fillColorPs, new ("FillColor.hlsl", "FillColorPS", ShaderStage.Pixel)),
            new (postProcessPS, new ("PostProcess.hlsl", "PostProcessPS", ShaderStage.Pixel)),
        ];

        static void Main()
        {
            bool res = ShaderCompiler.CompileShaders(engineShaderFiles, shadersSourcePath, outputFileName);

            Console.WriteLine(res ? "Shaders compiled successfully" : "Shaders compilation failed");
            Console.ReadKey();
        }
    }
}
