using System;

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
    }
}
