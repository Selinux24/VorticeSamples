using PrimalLike.Content;
using System.IO;
using TexturesImporter;

namespace DX12Windows.Assets
{
    static class Importer
    {
        public static void ImportAmbientOcclusionTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportBaseColorTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportEmissiveTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportMetalRoughTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.OutputFormat = BCFormats.BC5DualChannelGray;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportNormalTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(data);
                data.SaveTexture(importPath);
            }
        }

        internal static void ImportEnvironmentMapTexture(string texturePath, string importFolder, string brdfLutFile, string diffuseFile, string specularFile)
        {
            string brdfLutPath = Path.Combine(importFolder, brdfLutFile);
            string diffusePath = Path.Combine(importFolder, diffuseFile);
            string specularPath = Path.Combine(importFolder, specularFile);
            if (!File.Exists(brdfLutPath) || !File.Exists(diffusePath) || !File.Exists(specularPath))
            {
                using var data = new TextureData()
                {
                    ImportSettings = new()
                    {
                        Sources = texturePath,
                        OutputFormat = BCFormats.PickBestFit,
                        Compress = false,
                        PreferBc7 = true,
                        AlphaThreshold = 0.5f,
                        Dimension = TextureDimensions.TextureCube,
                        CubemapSize = 1024,
                        MirrorCubemap = true,
                        PrefilterCubemap = true
                    }
                };

                TextureImporter.Import(data);
                if (data.Info.ImportError != ImportErrors.Succeeded)
                {
                    return;
                }

                if (data.ImportSettings.PrefilterCubemap && data.Info.Flags.HasFlag(TextureFlags.IsCubeMap))
                {
                    using var brdf = new TextureData(data.ImportSettings);
                    TextureImporter.ComputeBrdfIntegrationLut(brdf);
                    brdf.SaveTexture(brdfLutPath);

                    using var diff = data.Copy();
                    TextureImporter.PrefilterIbl(diff, IblFilter.Diffuse);
                    diff.SaveTexture(diffusePath);

                    TextureImporter.PrefilterIbl(data, IblFilter.Specular);
                }

                data.SaveTexture(specularPath);
            }
        }
    }
}
