
namespace TexturesImporter
{
    struct TextureImportSettings()
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
        public uint OutputFormat = 0;
        public bool Compress = false;
    }
}
