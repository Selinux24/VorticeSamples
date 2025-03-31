using DirectXTexNet;
using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        private const string outputFolder = "./Content/";
        private const string outputTextureExt = ".texture";
        private const string outputIconExt = ".icon";

        private const string textureM24Path = "../../../../../Assets/M24.dds";
        private const string textureAmbientOcclusionPath = "../../../../../Assets/AmbientOcclusion.png";
        private const string textureBaseColorPath = "../../../../../Assets/BaseColor.png";
        private const string textureEmissivePath = "../../../../../Assets/Emissive.png";
        private const string textureMetalRoughPath = "../../../../../Assets/MetalRough.png";
        private const string textureNormalPath = "../../../../../Assets/Normal.png";

        static void Main()
        {
            Import(textureM24Path, DXGI_FORMAT.BC6H_UF16, true);
            Import(textureAmbientOcclusionPath);
            Import(textureBaseColorPath);
            Import(textureEmissivePath);
            Import(textureMetalRoughPath);
            Import(textureNormalPath);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        private static void Import(string texturePath, DXGI_FORMAT? format = null, bool compress = false)
        {
            if(!File.Exists(texturePath))
            {
                Console.WriteLine($"Texture not found => {Path.GetFileName(texturePath)}");
                return;
            }

            TextureData textureData = new();
            textureData.ImportSettings.Sources = texturePath;
            textureData.ImportSettings.Compress = compress;
            if (format.HasValue) textureData.ImportSettings.OutputFormat = (uint)format;

            TextureImporter.Import(ref textureData);

            textureData.SaveTexture(GetTexturePath(texturePath));
            textureData.SaveIcon(GetIconPath(texturePath));

            TextureImporter.ShutDownTextureTools();

            Console.WriteLine($"Texture imported successfully => {Path.GetFileName(texturePath)}");
        }
        private static string GetTexturePath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputTextureExt));
        }
        private static string GetIconPath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputIconExt));
        }
    }
}
