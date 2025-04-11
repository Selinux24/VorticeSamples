
using System.ComponentModel;

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
    }

    public enum BCFormats : uint
    {
        [Description("Pick Best Fit")]
        PickBestFit = 0,
        [Description("BC1 (RGBA) Low Quality Alpha")]
        BC1LowQualityAlpha = Vortice.DXGI.Format.BC1_UNorm,
        [Description("BC3 (RGBA) Medium Quality")]
        BC3MediumQuality = Vortice.DXGI.Format.BC3_UNorm,
        [Description("BC4 (R8) Single-Channel Gray")]
        BC4SingleChannelGray = Vortice.DXGI.Format.BC4_UNorm,
        [Description("BC5 (R8G8) Dual-Channel Gray")]
        BC5DualChannelGray = Vortice.DXGI.Format.BC5_UNorm,
        [Description("BC6 (UF16) HDR")]
        BC6HDR = Vortice.DXGI.Format.BC6H_Uf16,
        [Description("BC7 (RGBA) High Quality")]
        BC7HighQuality = Vortice.DXGI.Format.BC7_UNorm,
    }
}
