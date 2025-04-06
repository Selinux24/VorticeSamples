using System;
using System.IO;
using Utilities;

namespace TexturesImporter
{
    public struct TextureData()
    {
        public const uint MaxMips = 14; // we support up to 8k textures.
        public IntPtr SubresourceData = 0;
        public uint SubresourceSize = 0;
        public IntPtr Icon = 0;
        public uint IconSize = 0;
        public TextureInfo Info = new();
        public TextureImportSettings ImportSettings = new();

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

            FileUtils.MakeRoom(outputPath);

            using var file = File.OpenWrite(outputPath);
            using var writer = new BinaryWriter(file);
            writer.Write(Info.Width);
            writer.Write(Info.Height);
            writer.Write(Info.ArraySize);
            writer.Write((uint)Info.Flags);
            writer.Write(Info.MipLevels);
            writer.Write(Info.Format);

            FileUtils.WriteFile(writer, SubresourceData, SubresourceSize);
        }
    }
}
