
namespace PrimalLike.Content
{
    public enum TextureFlags : uint
    {
        IsHdr = 0x01,
        HasAlpha = 0x02,
        IsPremultipliedAlpha = 0x04,
        IsImportedAsNormalMap = 0x08,
        IsCubeMap = 0x10,
        IsVolumeMap = 0x20,
        IsSRGB = 0x40,
    }
}
