using System;
using System.Diagnostics;

namespace Utilities
{
    public static class OS
    {
        public static bool IsWindows11OrGreater()
        {
            Debug.WriteLine(Environment.OSVersion);

            //Detect windows 11
            if (Environment.OSVersion.Version.Major >= 10 &&
                Environment.OSVersion.Version.Minor >= 0 &&
                Environment.OSVersion.Version.Build >= 22000)
            {
                return true;
            }

            return false;
        }
    }
}
