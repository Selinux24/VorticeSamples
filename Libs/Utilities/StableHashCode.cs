using System;
using System.Runtime.InteropServices;

namespace Utilities
{
    public static class StableHashCode
    {
        const int HASH_OFFSET = 5381;
        const int HASH_MULTIPLIER = 1566083941;

        public static int Get<T>(T data) where T : unmanaged
        {
            // Get byte array from data struct
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(data, ptr, false);
            byte[] byteArray = new byte[Marshal.SizeOf<T>()];
            Marshal.Copy(ptr, byteArray, 0, Marshal.SizeOf<T>());

            unchecked
            {
                int hash1 = HASH_OFFSET;
                int hash2 = HASH_OFFSET;

                for (int i = 0; i < byteArray.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ byteArray[i];
                    if (i == byteArray.Length - 1)
                    {
                        break;
                    }
                    hash2 = ((hash2 << 5) + hash2) ^ byteArray[i + 1];
                }

                return hash1 + (hash2 * HASH_MULTIPLIER);
            }
        }
    }
}
