using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

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
        public static void Write<T>(T[] data, IntPtr destination) where T : unmanaged
        {
            uint sizeInBytes = (uint)(sizeof(T) * data.Length);
            fixed (T* dataPtr = data)
            {
                NativeMemory.Copy(dataPtr, (T*)destination, sizeInBytes);
            }
        }
        public static void Write<T>(T[] data, ID3D12Resource destination) where T : unmanaged
        {
            // NOTE: range's Begin and End fields are set to 0, to indicate that
            //       the CPU is not reading any data (i.e. write-only)
            T* cpuAddress = default;
            D3D12Helpers.DxCall(destination.Map(0, cpuAddress));
            Debug.Assert(cpuAddress != null);

            uint sizeInBytes = (uint)(sizeof(T) * data.Length);
            fixed (T* dataPtr = data)
            {
                NativeMemory.Copy(dataPtr, cpuAddress, sizeInBytes);
            }

            destination.Unmap(0, null);
        }
    }
}
