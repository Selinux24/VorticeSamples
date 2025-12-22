using PrimalLike.Content;
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
            ImportEnvMap(textureEnvMap, 1024, true, true);
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

            TextureImportSettings settings = new()
            {
                Sources = texturePath,
                OutputFormat = format,
                Compress = compress,
                PreferBc7 = preferBc7,
                AlphaThreshold = 0.5f,
                Dimension = TextureDimensions.Texture2D
            };

            ImportInternal(settings);
        }
        private static void ImportEnvMap(string fileName, int size, bool mirror, bool prefilter, BCFormats format = BCFormats.PickBestFit, bool compress = false, bool preferBc7 = true)
        {
            string texturePath = Path.Combine(assetFolder, fileName);
            if (!File.Exists(texturePath))
            {
                Console.WriteLine($"Texture not found => {Path.GetFileName(texturePath)}");
                return;
            }

            TextureImportSettings settings = new()
            {
                Sources = texturePath,
                OutputFormat = format,
                Compress = compress,
                PreferBc7 = preferBc7,
                AlphaThreshold = 0.5f,
                Dimension = TextureDimensions.TextureCube,
                CubemapSize = size,
                MirrorCubemap = mirror,
                PrefilterCubemap = prefilter
            };

            ImportInternal(settings);
        }

        private static void ImportInternal(TextureImportSettings settings)
        {
            using var textureData = new TextureData(settings);
            TextureImporter.Import(textureData);
            Console.WriteLine($"{textureData.Info.ImportError} => {Path.GetFileName(textureData.ImportSettings.Sources)}");

            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                Console.WriteLine($"Texture import error: {textureData.Info.ImportError}");
                return;
            }

            textureData.SaveTexture(GetTexturePath(textureData.ImportSettings.Sources));
            Console.WriteLine($"  ASSET - Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels}");

            if (textureData.ImportSettings.PrefilterCubemap && textureData.Info.Flags.HasFlag(TextureFlags.IsCubeMap))
            {
                using var diffuseData = textureData.Copy();
                Prefilter(diffuseData, IblFilter.Diffuse);

                using var specularData = textureData.Copy();
                Prefilter(specularData, IblFilter.Specular);

                using var brdf = new TextureData(settings);
                BrdfIntegrationLut(brdf);
            }
        }
        private static string GetTexturePath(string textureName)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, outputTextureExt));
        }

        private static void Prefilter(TextureData textureData, IblFilter filter)
        {
            TextureImporter.PrefilterIbl(textureData, filter);
            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                Console.WriteLine($"Texture import error: {textureData.Info.ImportError}");
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

        private static void BrdfIntegrationLut(TextureData textureData)
        {
            textureData.ImportSettings.Compress = false;
            textureData.ImportSettings.MipLevels = 1;

            TextureImporter.ComputeBrdfIntegrationLut(textureData);
            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                Console.WriteLine($"Texture import error: {textureData.Info.ImportError}");
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
