using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Direct3D12.Helpers
{
    unsafe static class BuffersHelper
    {
        public static void Write<T>(T* p, T data) where T : unmanaged
        {
            uint size = (uint)Marshal.SizeOf<T>();

            Unsafe.CopyBlockUnaligned(p, Unsafe.AsPointer(ref data), size);
        }
        public static void WriteArray<T>(byte* address, T[] data) where T : unmanaged
        {
            WriteArray((T*)address, data);
        }
        public static void WriteArray<T>(T* p, T[] data) where T : unmanaged
        {
            uint size = (uint)Marshal.SizeOf<T>();

            for (uint i = 0; i < data.Length; i++)
            {
                Unsafe.CopyBlockUnaligned(
                    p,
                    Unsafe.AsPointer(ref data[i]),
                    size);

                p++;
            }
        }
    }
}
