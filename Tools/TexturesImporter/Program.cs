using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        private const string outputFolder = "./Content/";
        private const string outputTextureExt = ".texture";

        private const string textureM24Path = "../../../../../Assets/M24.dds";
        private const string textureAmbientOcclusionPath = "../../../../../Assets/AmbientOcclusion.png";
        private const string textureBaseColorPath = "../../../../../Assets/BaseColor.png";
        private const string textureEmissivePath = "../../../../../Assets/Emissive.png";
        private const string textureMetalRoughPath = "../../../../../Assets/MetalRough.png";
        private const string textureNormalPath = "../../../../../Assets/Normal.png";

        static void Main()
        {
            Import(textureAmbientOcclusionPath);
            Import(textureBaseColorPath);
            Import(textureEmissivePath);
            Import(textureMetalRoughPath);
            Import(textureNormalPath);
            Import(textureM24Path, BCFormats.BC6HDR);

            TextureImporter.ShutDownTextureTools();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
        private static void Import(string texturePath, BCFormats? format = null, bool compress = true, bool preferBc7 = true)
        {
            if (!File.Exists(texturePath))
            {
                Console.WriteLine($"Texture not found => {Path.GetFileName(texturePath)}");
                return;
            }

            TextureData textureData = new();
            textureData.ImportSettings.Sources = texturePath;
            if (format.HasValue) textureData.ImportSettings.OutputFormat = format.Value;
            textureData.ImportSettings.Compress = compress;
            textureData.ImportSettings.PreferBc7 = preferBc7;
            textureData.ImportSettings.AlphaThreshold = 0.5f;

            TextureImporter.Import(ref textureData);

            textureData.SaveTexture(GetTexturePath(texturePath));

            Console.WriteLine($"Texture imported successfully => {Path.GetFileName(texturePath)}");
        }
        private static string GetTexturePath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputTextureExt));
        }
    }
}
