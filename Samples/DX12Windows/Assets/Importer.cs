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
                TextureData data = new();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(ref data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportBaseColorTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                TextureData data = new();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(ref data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportEmissiveTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                TextureData data = new();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(ref data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportMetalRoughTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                TextureData data = new();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.OutputFormat = BCFormats.BC5DualChannelGray;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(ref data);
                data.SaveTexture(importPath);
            }
        }
        public static void ImportNormalTexture(string texturePath, string importFolder, string importFile)
        {
            string importPath = Path.Combine(importFolder, importFile);
            if (!File.Exists(importPath))
            {
                TextureData data = new();
                data.ImportSettings.Compress = true;
                data.ImportSettings.PreferBc7 = true;
                data.ImportSettings.AlphaThreshold = 0.5f;
                data.ImportSettings.Sources = texturePath;

                TextureImporter.Import(ref data);
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
                TextureData data = new()
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

                TextureImporter.Import(ref data);
                data.SaveTexture(specularPath);

                var spec = data.Copy();
                TextureImporter.PrefilterIbl(ref spec, IblFilter.Diffuse);
                spec.SaveTexture(diffusePath);

                TextureData brdf = new()
                {
                    ImportSettings = data.ImportSettings
                };
                TextureImporter.ComputeBrdfIntegrationLut(ref brdf);
                brdf.SaveTexture(brdfLutPath);
            }
        }
    }
}
