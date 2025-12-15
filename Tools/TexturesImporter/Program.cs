using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        private const string outputFolder = "./Content/";
        private const string outputTextureExt = ".texture";

        private const string textureCubeHorn = "../../../../../Assets/horn-koppe_spring.jpg";
        private const string textureAmbientOcclusionPath = "../../../../../Assets/AmbientOcclusion.png";
        private const string textureBaseColorPath = "../../../../../Assets/BaseColor.png";
        private const string textureEmissivePath = "../../../../../Assets/Emissive.png";
        private const string textureMetalRoughPath = "../../../../../Assets/MetalRough.png";
        private const string textureNormalPath = "../../../../../Assets/Normal.png";
        private const string textureM24Path = "../../../../../Assets/M24.dds";

        static void Main()
        {
            ImportCube(textureCubeHorn, 256);
            Import(textureAmbientOcclusionPath);
            Import(textureBaseColorPath);
            Import(textureEmissivePath);
            Import(textureMetalRoughPath);
            Import(textureNormalPath);
            Import(textureM24Path, BCFormats.BC6HDR);

            TextureImporter.ShutDownTextureTools();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
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

            ImportInternal(ref textureData);
        }
        private static void ImportCube(string texturePath, int size, BCFormats? format = null, bool compress = true, bool preferBc7 = true)
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
            textureData.ImportSettings.Dimension = TextureDimensions.TextureCube;
            textureData.ImportSettings.CubemapSize = size;
            textureData.ImportSettings.MirrorCubemap = true;
            textureData.ImportSettings.PrefilterCubemap = false;

            ImportInternal(ref textureData);
        }
        private static void ImportInternal(ref TextureData textureData)
        {
            TextureImporter.Import(ref textureData);

            textureData.SaveTexture(GetTexturePath(textureData.ImportSettings.Sources));

            Console.WriteLine($"{textureData.Info.ImportError} => {Path.GetFileName(textureData.ImportSettings.Sources)}");
            if (textureData.Info.ImportError == ImportErrors.Succeeded)
            {
                Console.WriteLine($"  Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels}");
            }
        }
        private static string GetTexturePath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputTextureExt));
        }
    }
}
