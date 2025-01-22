using Direct3D12;
using Direct3D12.Shaders;
using PrimalLike.Components;
using ShaderCompiler;
using System;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        private const string shadersSourceDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersOutputPath = "./Content/engineShaders.bin";

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new ("FullScreenTriangle.hlsl", "FullScreenTriangleVS", ShaderStage.Vertex)),
            new ((int)EngineShaders.FillColorPs, new ("FillColor.hlsl", "FillColorPS", ShaderStage.Pixel)),
            new ((int)EngineShaders.PostProcessPs, new ("PostProcess.hlsl", "PostProcessPS", ShaderStage.Pixel)),
        ];

        static void Main()
        {
            if (!GameEntity.RegisterScript<TestScript>())
            {
                Console.WriteLine("Failed to register TestScript");
            }

            if (!Compiler.CompileShaders(engineShaderFiles, shadersSourceDir, shadersOutputPath))
            {
                Console.WriteLine("Engine shaders compilation failed");
            }
            
            var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();

            Win32WindowInfo windowInfo = new()
            {
                Title = "DX12 for Windows",
                ClientArea = new(50, 50, 800, 600),
                IsFullScreen = false,
            };
            app.CreateWindow(windowInfo);

            app.Run();
        }
    }
}
