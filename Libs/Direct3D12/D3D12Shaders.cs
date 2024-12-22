using PrimalLike;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Direct3D12
{
    class D3D12Shaders
    {
        [StructLayout(LayoutKind.Sequential)]
        struct CompiledShader
        {
            public int Size;
            public IntPtr ByteCode;
        }

        private static readonly CompiledShader?[] engineShaders = new CompiledShader?[Enum.GetValues(typeof(EngineShaders)).Length];
        private static byte[] shadersBlob;

        public static bool Initialize()
        {
            return LoadEngineShaders();
        }
        private static bool LoadEngineShaders()
        {
            Debug.Assert(shadersBlob == null);
            bool result = Engine.LoadEngineShaders(out shadersBlob);
            Debug.Assert(shadersBlob != null && shadersBlob.Length > 0);

            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;

            using var stream = new MemoryStream(shadersBlob);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);
            for (uint i = 0; i < egCount; i++)
            {
                int size = reader.ReadInt32();
                byte[] data = reader.ReadBytes(size);

                nint pt = Marshal.AllocHGlobal(size);
                Marshal.Copy(data, 0, pt, size);

                engineShaders[i] = new()
                {
                    Size = size,
                    ByteCode = pt
                };
            }

            Debug.Assert(reader.BaseStream.Position == reader.BaseStream.Length);

            return result;
        }

        public static void Shutdown()
        {
            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;
            for (uint i = 0; i < egCount; i++)
            {
                engineShaders[i] = null;
            }
            shadersBlob = null;
        }

        public static D3D12ShaderBytecode GetEngineShader(EngineShaders id)
        {
            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;
            Debug.Assert((int)id < egCount);
            CompiledShader? shader = engineShaders[(int)id];
            Debug.Assert(shader.HasValue && shader.Value.Size > 0);
            return new D3D12ShaderBytecode
            {
                ByteCodeLength = shader.Value.Size,
                ByteCode = shader.Value.ByteCode,
            };
        }
    }
}
