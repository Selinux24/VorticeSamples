using DirectXTexNet;
using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        private const string textureM24Path = "../../../../../Assets/M24.dds";
        private const string outputTexture = "./Content/M24.texture";
        private const string outputIcon = "./Content/M24.icon";

        static void Main()
        {
            TextureData textureData = new();
            textureData.ImportSettings.Sources = textureM24Path;
            textureData.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC6H_UF16;
            textureData.ImportSettings.Compress = true;

            TextureImporter.Import(ref textureData);

            textureData.SaveTexture(outputTexture);
            textureData.SaveIcon(outputIcon);

            TextureImporter.ShutDownTextureTools();

            Console.WriteLine($"Texture imported successfully => {Path.GetFileName(textureM24Path)}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
