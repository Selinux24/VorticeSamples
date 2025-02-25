using DirectXTexNet;
using System;

namespace TexturesImporter
{
    internal class Program
    {
        private const string textureM24Path = "../../../../../Assets/M24.dds";

        static void Main()
        {
            TextureData textureData = new();
            textureData.ImportSettings.Sources = textureM24Path;
            textureData.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC6H_UF16;
            textureData.ImportSettings.Compress = true;

            TextureImporter.Import(ref textureData);

            TextureImporter.ShutDownTextureTools();

            Console.ReadKey();
        }
    }
}
