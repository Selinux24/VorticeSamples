using DirectXTexNet;
using PrimalLike.Content;
using System;
using System.IO;

namespace TexturesImporter
{
    internal class Program
    {
        const string outputFolder = "./Content/";
        const string outputTextureExt = ".texture";
        const string outputDiffuseExt = ".diffuse";
        const string outputBrdfLutExt = ".brdf";

        const string assetFolder = "../../../../../Assets/";
        const string textureEnvMap = "sunny_rose_garden_4k.hdr";
        const string textureAmbientOcclusion = "AmbientOcclusion.png";
        const string textureBaseColor = "BaseColor.png";
        const string textureEmissive = "Emissive.png";
        const string textureMetalRough = "MetalRough.png";
        const string textureNormal = "Normal.png";
        const string textureHumvee = "humvee.jpg";
        const string textureM24 = "M24.dds";

        static void Main()
        {
            using var importer = new TextureImporter();

            ImportEnvMap(importer, textureEnvMap, 1024, true, true);
            Import(importer, textureAmbientOcclusion);
            Import(importer, textureBaseColor);
            Import(importer, textureEmissive);
            Import(importer, textureMetalRough, BCFormats.BC5DualChannelGray);
            Import(importer, textureNormal);
            Import(importer, textureHumvee);
            Import(importer, textureM24, BCFormats.BC6HDR);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(true);
        }

        static void Import(TextureImporter importer, string fileName, BCFormats format = BCFormats.PickBestFit, bool compress = true, bool preferBc7 = true)
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

            ImportInternal(importer, settings);
        }
        static void ImportEnvMap(TextureImporter importer, string fileName, int size, bool mirror, bool prefilter, BCFormats format = BCFormats.PickBestFit, bool compress = false, bool preferBc7 = true)
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

            ImportInternal(importer, settings);
        }

        static void ImportInternal(TextureImporter importer, TextureImportSettings settings)
        {
            using var textureData = new TextureData(settings);
            importer.Import(textureData);
            if (!EvualuateResult(textureData)) return;

            if (textureData.ImportSettings.PrefilterCubemap && textureData.Info.Flags.HasFlag(TextureFlags.IsCubeMap))
            {
                using var brdf = new TextureData(settings);
                brdf.ImportSettings.Compress = false;
                brdf.ImportSettings.MipLevels = 1;
                importer.ComputeBrdfIntegrationLut(brdf);
                if (!EvualuateResult(brdf)) return;
                brdf.SaveTexture(GetTexturePath(brdf.ImportSettings.Sources, outputBrdfLutExt));
                Console.WriteLine($"  BRDFL - Size: {brdf.Info.Width}x{brdf.Info.Height}, ArraySize: {brdf.Info.ArraySize}, Mips: {brdf.Info.MipLevels} - {(DXGI_FORMAT)brdf.Info.Format}");

                using var diffuseData = textureData.Copy();
                importer.PrefilterIbl(diffuseData, IblFilter.Diffuse);
                if (!EvualuateResult(diffuseData)) return;
                diffuseData.SaveTexture(GetTexturePath(diffuseData.ImportSettings.Sources, outputDiffuseExt));
                Console.WriteLine($"  FDIFF - Size: {diffuseData.Info.Width}x{diffuseData.Info.Height}, ArraySize: {diffuseData.Info.ArraySize}, Mips: {diffuseData.Info.MipLevels} - {(DXGI_FORMAT)diffuseData.Info.Format}");

                importer.PrefilterIbl(textureData, IblFilter.Specular);
                if (!EvualuateResult(textureData)) return;
            }

            textureData.SaveTexture(GetTexturePath(textureData.ImportSettings.Sources, outputTextureExt));
            Console.WriteLine($"  ASSET - Size: {textureData.Info.Width}x{textureData.Info.Height}, ArraySize: {textureData.Info.ArraySize}, Mips: {textureData.Info.MipLevels} - {(DXGI_FORMAT)textureData.Info.Format}");
        }
        static bool EvualuateResult(TextureData textureData)
        {
            if (textureData.Info.ImportError != ImportErrors.Succeeded)
            {
                Console.WriteLine($"Texture import error: {textureData.Info.ImportError}");
                return false;
            }

            return true;
        }
        static string GetTexturePath(string textureName, string extension)
        {
            string fileName = Path.GetFileName(textureName);
            return Path.Combine(outputFolder, Path.ChangeExtension(fileName, extension));
        }
    }
}
