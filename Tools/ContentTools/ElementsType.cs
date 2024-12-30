
namespace ContentTools
{
    /// <summary>
    /// Enumerates the different types of elements that can be used in a model.
    /// </summary>
    public enum ElementsType : uint
    {
        PositionOnly = 0x00,
        StaticNormal = 0x01,
        StaticNormalTexture = 0x03,
        StaticColor = 0x04,
        Skeletal = 0x08,
        SkeletalColor = Skeletal | StaticColor,
        SkeletalNormal = Skeletal | StaticNormal,
        SkeletalNormalColor = Skeletal | StaticNormal | StaticColor,
        SkeletalNormalTexture = Skeletal | StaticNormalTexture,
        SkeletalNormalTextureColor = Skeletal | StaticNormalTexture | StaticColor,
    }
}
