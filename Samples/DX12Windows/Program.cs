using Direct3D12;
using PrimalLike.Components;
using ShaderCompiler;
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
            ShaderCompilation.CompileShaders(engineShaderFiles, shadersSourceDir, shadersOutputPath);

            Win32WindowInfo windowInfo = new()
            {
                Title = "DX12 for Windows",
                ClientArea = new System.Drawing.Rectangle(250, 250, 800, 600),
                IsFullScreen = false,
            };

            GameEntity.RegisterScript<TestScript>();

            var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            app.CreateWindow(windowInfo);

            app.Run();
        }
    }
}
