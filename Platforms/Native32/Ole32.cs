using System;
using System.Runtime.InteropServices;

namespace Native32
{
    static partial class Ole32
    {
        const string LibraryName = "ole32.dll";

        [LibraryImport(LibraryName, SetLastError = true)]
        public static partial int CoInitializeEx(IntPtr pvReserved, COINIT dwCoInit);

        [LibraryImport(LibraryName, SetLastError = true)]
        public static partial int CoUninitialize();

        /// <summary>
        /// https://www.pinvoke.net/default.aspx/ole32.coinitializeex
        /// </summary>
        [Flags]
        public enum COINIT : uint
        {
            /// <summary>
            /// Initializes the thread for multi-threaded object concurrency.
            /// </summary>
            COINIT_MULTITHREADED = 0x0,
            /// <summary>
            /// Initializes the thread for apartment-threaded object concurrency
            /// </summary>
            COINIT_APARTMENTTHREADED = 0x2,
            /// <summary>
            /// Disables DDE for OLE1 support
            /// </summary>
            COINIT_DISABLE_OLE1DDE = 0x4,
            /// <summary>
            /// Trade memory for speed
            /// </summary>
            COINIT_SPEED_OVER_MEMORY = 0x8,
        }
    }
}
