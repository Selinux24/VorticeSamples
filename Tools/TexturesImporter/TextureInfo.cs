using PrimalLike.Content;

namespace TexturesImporter
{
    struct TextureInfo()
    {
        public int Width = 0;
        public int Height = 0;
        public int ArraySize = 0;
        public int MipLevels = 0;
        public uint Format = 0;
        public ImportErrors ImportError = 0;
        public TextureFlags Flags = 0;
    }
}
