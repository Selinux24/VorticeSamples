
namespace TexturesImporter
{
    public struct TextureImportSettings()
    {
        /// <summary>
        /// String of one or more file paths separated by semi-colons ';'
        /// </summary>
        public string Sources = null;
        /// <summary>
        /// Number of file paths
        /// </summary>
        public readonly uint SourceCount
        {
            get
            {
                return (uint)(Sources?.Split(';').Length ?? 0);
            }
        }
        public TextureDimensions Dimension = 0;
        public int MipLevels = 0;
        public float AlphaThreshold = 0;
        public bool PreferBc7 = false;
        public BCFormats OutputFormat = 0;
        public bool Compress = false;
        public int CubemapSize;
        public bool MirrorCubemap;
        public bool PrefilterCubemap;

        public void FromContentSettings(TextureImportSettings settings)
        {
            Sources = string.Join(";", settings.Sources);
            Dimension = settings.Dimension;
            MipLevels = settings.MipLevels;
            AlphaThreshold = settings.AlphaThreshold;
            PreferBc7 = settings.PreferBc7;
            OutputFormat = settings.OutputFormat;
            Compress = settings.Compress;
            CubemapSize = settings.CubemapSize;
            MirrorCubemap = settings.MirrorCubemap;
            PrefilterCubemap = settings.PrefilterCubemap;
        }
    }
}
