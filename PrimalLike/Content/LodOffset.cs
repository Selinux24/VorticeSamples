using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct LodOffset(ushort offset, ushort count)
    {
        [FieldOffset(0)] public ushort Offset = offset;
        [FieldOffset(2)] public ushort Count = count;

        public static int Stride => Marshal.SizeOf<LodOffset>();
    }
}
