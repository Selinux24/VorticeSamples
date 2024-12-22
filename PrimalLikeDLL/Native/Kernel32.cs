using System;
using System.Runtime.InteropServices;

namespace PrimalLikeDLL.Native
{
    static partial class Kernel32
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string dllToLoad);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool FreeLibrary(IntPtr hModule);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPWStr)] string procedureName);
    }
}
