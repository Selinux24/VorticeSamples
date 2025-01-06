using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ContentTools
{
    public class SceneData(string name)
    {
        public string Name { get; set; } = name ?? "Imported Scene";
        public IntPtr Buffer { get; set; }
        public int BufferSize { get; set; }
        public GeometryImportSettings Settings { get; set; } = new();

        public void SaveToFile(string fileName)
        {
            File.WriteAllBytes(fileName, GetBytes());
        }
        public byte[] GetBytes()
        {
            byte[] buffer = new byte[BufferSize];
            Marshal.Copy(Buffer, buffer, 0, BufferSize);
            return buffer;
        }
    }
}
