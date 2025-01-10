using PrimalLike;
using PrimalLike.Content;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Direct3D12
{
    class D3D12Shaders
    {
        private static readonly CompiledShader?[] engineShaders = new CompiledShader?[Enum.GetValues(typeof(EngineShaders)).Length];
        private static byte[] engineShadersBlob;

        public static bool Initialize()
        {
            return LoadEngineShaders();
        }
        private static bool LoadEngineShaders()
        {
            Debug.Assert(engineShadersBlob == null);
            bool result = Engine.LoadEngineShaders(out engineShadersBlob);
            Debug.Assert(engineShadersBlob != null && engineShadersBlob.Length > 0);

            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;

            using var stream = new MemoryStream(engineShadersBlob);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);
            for (uint i = 0; i < egCount; i++)
            {
                int size = reader.ReadInt32();
                byte[] hash = reader.ReadBytes(16);
                byte[] data = reader.ReadBytes(size);

                nint pt = Marshal.AllocHGlobal(size);
                Marshal.Copy(data, 0, pt, size);

                engineShaders[i] = new()
                {
                    ByteCode = data
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
            engineShadersBlob = null;
        }

        public static ReadOnlyMemory<byte> GetEngineShader(EngineShaders id)
        {
            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;
            Debug.Assert((int)id < egCount);
            CompiledShader? shader = engineShaders[(int)id];
            Debug.Assert(shader.HasValue && shader.Value.IsValid());

            return shader.Value.ByteCode;
        }
    }
}
