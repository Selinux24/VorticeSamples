using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    struct LodOffset
    {
        public ushort Offset;
        public ushort Count;

        public static int Stride => Marshal.SizeOf<IdType>();
    }
}
