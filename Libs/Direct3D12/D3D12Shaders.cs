using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Direct3D12
{
    class D3D12Shaders
    {
        [StructLayout(LayoutKind.Sequential)]
        struct CompiledShader
        {
            public ulong Size;
            public IntPtr ByteCode;
        }

        private static readonly CompiledShader?[] engineShaders = new CompiledShader?[Enum.GetValues(typeof(EngineShaders)).Length];
        private static byte[] shadersBlob;

        public static bool Initialize(string path)
        {
            return LoadEngineShaders(path);
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
                ByteCode = shader.Value.ByteCode,
                ByteCodeLength = shader.Value.Size
            };
        }

        private static bool LoadEngineShaders(string path)
        {
            Debug.Assert(shadersBlob == null);
            bool result = Engine.Core.Engine.LoadEngineShaders(path, out shadersBlob);
            Debug.Assert(shadersBlob != null && shadersBlob.Length > 0);

            int egCount = Enum.GetValues(typeof(EngineShaders)).Length;
            ulong offset = 0;
            uint index = 0;
            while (offset < (ulong)shadersBlob.Length && result)
            {
                Debug.Assert(index < egCount);
                ref CompiledShader? shader = ref engineShaders[index];
                Debug.Assert(shader == null);
                result &= index < egCount && shader == null;
                if (!result) break;
                var bytes = shadersBlob.AsSpan((int)offset).ToArray();
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                shader = Marshal.PtrToStructure<CompiledShader>(unmanagedPointer);
                offset += (ulong)Marshal.SizeOf<CompiledShader>() + shader.Value.Size;
                index++;
            }
            Debug.Assert(offset == (ulong)shadersBlob.Length && index == egCount);

            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct D3D12ShaderBytecode
    {
        public IntPtr ByteCode;
        public ulong ByteCodeLength;
    }
}
