using AssetsImporter;
using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TexturesImporter;

namespace DX12Windows.Assets
{
    static class Importer
    {
        public static void ImportModels(Func<IEnumerable<string>> assetGen, params string[] modelNames)
        {
            if (!modelNames.Any(f => !File.Exists(f)))
            {
                return;
            }

            var assets = assetGen().ToArray();

            Debug.Assert(assets.Length == modelNames.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (string.IsNullOrEmpty(assets[i]))
                {
                    continue;
                }

                AssimpImporter.PackForEngine(assets[i], modelNames[i]);
            }
        }

        public static void ImportAmbientOcclusionTexture(TextureImporter importer, string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                importer.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportBaseColorTexture(TextureImporter importer, string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                importer.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportEmissiveTexture(TextureImporter importer, string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                importer.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportMetalRoughTexture(TextureImporter importer, string texturePath, string importFolder, string importFile)
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

                importer.Import(data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportNormalTexture(TextureImporter importer, string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                using var data = new TextureData();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                importer.Import(data);
                data.SaveTexture(importPath);
            }
        }

        public static void ImportEnvironmentMapTexture(TextureImporter importer, string texturePath, string brdfLutPath, string diffusePath, string specularPath)
        {
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

                importer.Import(data);
                if (data.Info.ImportError != ImportErrors.Succeeded)
                {
                    return;
                }

                if (data.ImportSettings.PrefilterCubemap && data.Info.Flags.HasFlag(TextureFlags.IsCubeMap))
                {
                    using var brdf = new TextureData(data.ImportSettings);
                    importer.ComputeBrdfIntegrationLut(brdf);
                    brdf.SaveTexture(brdfLutPath);

                    using var diff = data.Copy();
                    importer.PrefilterIbl(diff, IblFilter.Diffuse);
                    diff.SaveTexture(diffusePath);

                    importer.PrefilterIbl(data, IblFilter.Specular);
                }

                data.SaveTexture(specularPath);
            }
        }
    }
}
