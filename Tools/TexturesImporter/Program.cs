using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        private const string outputFolder = "./Content/";
        private const string outputTextureExt = ".texture";
        private const string outputDiffuseExt = ".diffuse";
        private const string outputSpecularExt = ".specular";
        private const string outputBrdfLutExt = ".brdf";

        private const string assetFolder = "../../../../../Assets/";
        private const string textureEnvMap = "sunny_rose_garden_4k.hdr";
        private const string textureAmbientOcclusion = "AmbientOcclusion.png";
        private const string textureBaseColor = "BaseColor.png";
        private const string textureEmissive = "Emissive.png";
        private const string textureMetalRough = "MetalRough.png";
        private const string textureNormal = "Normal.png";
        private const string textureHumvee = "humvee.jpg";
        private const string textureM24 = "M24.dds";

        static void Main()
        {
            ImportEnvMap(textureEnvMap, 256, true, true);
            Import(textureAmbientOcclusion);
            Import(textureBaseColor);
            Import(textureEmissive);
            Import(textureMetalRough, BCFormats.BC5DualChannelGray);
            Import(textureNormal);
            Import(textureHumvee);
            Import(textureM24, BCFormats.BC6HDR);

            TextureImporter.ShutDownTextureTools();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
        }

        private static void Import(string fileName, BCFormats format = BCFormats.PickBestFit, bool compress = true, bool preferBc7 = true)
        {
            string texturePath = Path.Combine(assetFolder, fileName);
            if (!File.Exists(texturePath))
            {
                Console.WriteLine($"Texture not found => {Path.GetFileName(texturePath)}");
                return;
            }

            TextureData textureData = new();
            textureData.ImportSettings.Sources = texturePath;
            textureData.ImportSettings.OutputFormat = format;
            textureData.ImportSettings.Compress = compress;
            textureData.ImportSettings.PreferBc7 = preferBc7;
            textureData.ImportSettings.AlphaThreshold = 0.5f;
            textureData.ImportSettings.Dimension = TextureDimensions.Texture2D;

            ImportInternal(ref textureData);
        }
        private static void ImportEnvMap(string fileName, int size, bool mirror, bool prefilter, BCFormats format = BCFormats.PickBestFit, bool compress = false, bool preferBc7 = true)
        {
            string texturePath = Path.Combine(assetFolder, fileName);
            if (!File.Exists(texturePath))
            {
                Console.WriteLine($"Texture not found => {Path.GetFileName(texturePath)}");
                return;
            }

            TextureData textureData = new();
            textureData.ImportSettings.Sources = texturePath;
            textureData.ImportSettings.OutputFormat = format;
            textureData.ImportSettings.Compress = compress;
            textureData.ImportSettings.PreferBc7 = preferBc7;
            textureData.ImportSettings.AlphaThreshold = 0.5f;
            textureData.ImportSettings.Dimension = TextureDimensions.TextureCube;
            textureData.ImportSettings.CubemapSize = size;
            textureData.ImportSettings.MirrorCubemap = mirror;
            textureData.ImportSettings.PrefilterCubemap = prefilter;

            ImportInternal(ref textureData);

            if (textureData.ImportSettings.PrefilterCubemap)
            {
                var diff = textureData.Copy();
                var spec = textureData.Copy();
                var brdf = textureData.Copy();

                Prefilter(ref diff, IblFilter.Diffuse);
                Prefilter(ref spec, IblFilter.Specular);
                BrdfIntegrationLut(ref brdf);
            }
        }

        private static void ImportInternal(ref TextureData textureData)
        {
            TextureImporter.Import(ref textureData);
            Console.WriteLine($"{textureData.Info.ImportError} => {Path.GetFileName(textureData.ImportSettings.Sources)}");

            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                return;
            }

            textureData.SaveTexture(GetTexturePath(textureData.ImportSettings.Sources));
            Console.WriteLine($"  ASSET - Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels}");
        }
        private static string GetTexturePath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputTextureExt));
        }

        private static void Prefilter(ref TextureData textureData, IblFilter filter)
        {
            TextureImporter.PrefilterIbl(ref textureData, filter);
            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                return;
            }

            textureData.SaveTexture(GetTextureFilterPath(textureData.ImportSettings.Sources, filter));
            string filterStr = filter == IblFilter.Diffuse ? "FDIFF" : "FSPEC";
            Console.WriteLine($"  {filterStr} - Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels}");
        }
        private static string GetTextureFilterPath(string textureName, IblFilter filter)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, filter == IblFilter.Diffuse ? outputDiffuseExt : outputSpecularExt));
        }

        private static void BrdfIntegrationLut(ref TextureData textureData)
        {
            TextureImporter.ComputeBrdfIntegrationLut(ref textureData);
            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                return;
            }

            textureData.SaveTexture(GetTextureBrdfIntegrationLutPath(textureData.ImportSettings.Sources));
            Console.WriteLine($"  BRDFL - Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels}");
        }
        private static string GetTextureBrdfIntegrationLutPath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputBrdfLutExt));
        }
    }
}
