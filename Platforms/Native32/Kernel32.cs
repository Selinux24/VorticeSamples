using System.Runtime.InteropServices;

namespace Native32
{
    static partial class Kernel32
    {
        const string LibraryName = "kernel32.dll";

        [LibraryImport(LibraryName)]
        public static partial void SetLastError(uint dwErrCode);
        [LibraryImport(LibraryName)]
        public static partial uint GetLastError();
    }
}
