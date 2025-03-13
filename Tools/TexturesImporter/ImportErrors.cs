
namespace TexturesImporter
{
    public enum ImportErrors : uint
    {
        Succeeded = 0,
        Unknown,
        Compress,
        Decompress,
        Load,
        MipmapGeneration,
        MaxSizeExceeded,
        SizeMismatch,
        FormatMismatch,
        FileNotFound,
        NeedSixImages,
    }
}
