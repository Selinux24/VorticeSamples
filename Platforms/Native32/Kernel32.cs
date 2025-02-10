using System.Runtime.InteropServices;

namespace Native32
{
#pragma warning disable SYSLIB1054
#pragma warning disable CA1069
    static partial class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint dwErrCode);
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
    }
#pragma warning restore SYSLIB1054
#pragma warning restore CA1069
}
