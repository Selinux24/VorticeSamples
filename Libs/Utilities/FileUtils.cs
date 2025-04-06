using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Utilities
{
    public static class FileUtils
    {
        const int BatchSize = 1024 * 1024;

        /// <summary>
        /// Writes the data pointer to a file
        /// </summary>
        /// <param name="writer">Stream writer</param>
        /// <param name="ptr">Data pointer</param>
        /// <param name="size">Size</param>
        public static void WriteFile(BinaryWriter writer, IntPtr ptr, int size)
        {
            WriteFile(writer, ptr, (uint)size);
        }
        /// <summary>
        /// Writes the data pointer to a file
        /// </summary>
        /// <param name="writer">Stream writer</param>
        /// <param name="ptr">Data pointer</param>
        /// <param name="size">Size</param>
        public static void WriteFile(BinaryWriter writer, IntPtr ptr, uint size)
        {
            uint s = size;
            byte[] batchArray = new byte[BatchSize];
            while (s > 0)
            {
                int chunkSize = (int)(s > BatchSize ? BatchSize : s);

                Marshal.Copy(ptr, batchArray, 0, chunkSize);
                writer.Write(batchArray, 0, chunkSize);
                ptr += chunkSize;
                s -= (uint)chunkSize;
            }
        }
        /// <summary>
        /// Validates and prepares output path
        /// </summary>
        /// <param name="outputPath">Output path</param>
        public static void MakeRoom(string outputPath)
        {
            string fullPath = Path.GetFullPath(outputPath);

            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
