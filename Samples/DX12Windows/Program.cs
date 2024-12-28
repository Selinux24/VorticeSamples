using Direct3D12;
using PrimalLike.Components;
using ShaderCompiler;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        private const string shadersSourcePath = "../../../../../Libs/Direct3D12/Shaders/";
        private const string outputFileName = "./Content/engineShaders.bin";

        private static readonly string[] profileStrings = ["vs_6_5", "hs_6_5", "ds_6_5", "gs_6_5", "ps_6_5", "cs_6_5", "as_6_5", "ms_6_5"];

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new ("FullScreenTriangle.hlsl", "FullScreenTriangleVS", (int)D3D12ShaderTypes.Vertex, profileStrings[(int)D3D12ShaderTypes.Vertex])),
            new ((int)EngineShaders.FillColorPs, new ("FillColor.hlsl", "FillColorPS", (int)D3D12ShaderTypes.Pixel, profileStrings[(int)D3D12ShaderTypes.Pixel])),
            new ((int)EngineShaders.PostProcessPs, new ("PostProcess.hlsl", "PostProcessPS", (int)D3D12ShaderTypes.Pixel, profileStrings[(int)D3D12ShaderTypes.Pixel])),
        ];

        static void Main()
        {
            ShaderCompilation.CompileShaders(shadersSourcePath, engineShaderFiles, outputFileName);

            Win32WindowInfo windowInfo1 = new()
            {
                Title = "DX12 for Windows 1",
                ClientArea = new System.Drawing.Rectangle(100, 100, 400, 800),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo2 = new()
            {
                Title = "DX12 for Windows 2",
                ClientArea = new System.Drawing.Rectangle(150, 150, 800, 400),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo3 = new()
            {
                Title = "DX12 for Windows 3",
                ClientArea = new System.Drawing.Rectangle(200, 200, 400, 400),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo4 = new()
            {
                Title = "DX12 for Windows 4",
                ClientArea = new System.Drawing.Rectangle(250, 250, 800, 600),
                IsFullScreen = false,
            };

            GameEntity.RegisterScript<TestScript>();

            var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsFactory>();
            app.CreateWindow(windowInfo1);
            app.CreateWindow(windowInfo2);
            app.CreateWindow(windowInfo3);
            app.CreateWindow(windowInfo4);
            app.Run();
        }
    }
}
