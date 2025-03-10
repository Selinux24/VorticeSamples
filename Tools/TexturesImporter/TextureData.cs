using System;
using Utilities;

namespace TexturesImporter
{
    struct TextureData()
    {
        public const uint MaxMips = 14; // we support up to 8k textures.
        public IntPtr SubresourceData = 0;
        public uint SubresourceSize = 0;
        public IntPtr Icon = 0;
        public uint IconSize = 0;
        public TextureInfo Info = new();
        public TextureImportSettings ImportSettings = new();

        /// <summary>
        /// Saves the icon to a file
        /// </summary>
        /// <param name="outputPath">Output path</param>
        public readonly void SaveIcon(string outputPath)
        {
            if (Icon == IntPtr.Zero || IconSize == 0)
            {
                return;
            }

            FileUtils.WriteFile(Icon, IconSize, outputPath);
        }
        /// <summary>
        /// Saves the texture to a file
        /// </summary>
        /// <param name="outputPath">Output path</param>
        public readonly void SaveTexture(string outputPath)
        {
            if (SubresourceData == IntPtr.Zero || SubresourceSize == 0)
            {
                return;
            }

            FileUtils.WriteFile(SubresourceData, SubresourceSize, outputPath);
        }
    }
}
