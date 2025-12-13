using System.IO;
using TexturesImporter;

namespace DX12Windows.Assets
{
    static class Importer
    {
        public static void ImportAmbientOcclusionTexture(string importPath, string outputPath)
        {
            if (!File.Exists(importPath))
            {
                TextureData ambientOcclusionData = new();
                ambientOcclusionData.ImportSettings.Compress = true;
                ambientOcclusionData.ImportSettings.PreferBc7 = true;
                ambientOcclusionData.ImportSettings.AlphaThreshold = 0.5f;
                ambientOcclusionData.ImportSettings.Sources = outputPath;

                TextureImporter.Import(ref ambientOcclusionData);
                ambientOcclusionData.SaveTexture(importPath);
            }
        }
        public static void ImportBaseColorTexture(string importPath, string outputPath)
        {
            if (!File.Exists(importPath))
            {
                TextureData baseColorData = new();
                baseColorData.ImportSettings.Compress = true;
                baseColorData.ImportSettings.PreferBc7 = true;
                baseColorData.ImportSettings.AlphaThreshold = 0.5f;
                baseColorData.ImportSettings.Sources = outputPath;

                TextureImporter.Import(ref baseColorData);
                baseColorData.SaveTexture(importPath);
            }
        }
        public static void ImportEmissiveTexture(string importPath, string outputPath)
        {
            if (!File.Exists(importPath))
            {
                TextureData emissiveData = new();
                emissiveData.ImportSettings.Compress = true;
                emissiveData.ImportSettings.PreferBc7 = true;
                emissiveData.ImportSettings.AlphaThreshold = 0.5f;
                emissiveData.ImportSettings.Sources = outputPath;

                TextureImporter.Import(ref emissiveData);
                emissiveData.SaveTexture(importPath);
            }
        }
        public static void ImportMetalRoughTexture(string importPath, string outputPath)
        {
            if (!File.Exists(importPath))
            {
                TextureData metalRoughData = new();
                metalRoughData.ImportSettings.Compress = true;
                metalRoughData.ImportSettings.PreferBc7 = true;
                metalRoughData.ImportSettings.AlphaThreshold = 0.5f;
                metalRoughData.ImportSettings.OutputFormat = BCFormats.BC5DualChannelGray;
                metalRoughData.ImportSettings.Sources = outputPath;

                TextureImporter.Import(ref metalRoughData);
                metalRoughData.SaveTexture(importPath);
            }
        }
        public static void ImportNormalTexture(string importPath, string outputPath)
        {
            if (!File.Exists(importPath))
            {
                TextureData normalData = new();
                normalData.ImportSettings.Compress = true;
                normalData.ImportSettings.PreferBc7 = true;
                normalData.ImportSettings.AlphaThreshold = 0.5f;
                normalData.ImportSettings.Sources = outputPath;

                TextureImporter.Import(ref normalData);
                normalData.SaveTexture(importPath);
            }
        }
    }
}
