using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    struct LodOffset
    {
        [FieldOffset(0)] public ushort Offset;
        [FieldOffset(2)] public ushort Count;

        public static int Stride => Marshal.SizeOf<LodOffset>();
    }
}
