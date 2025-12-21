using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AmbientLightParameters()
    {
        public float Intensity = -1f;
        public uint DiffuseSrvIndex = uint.MaxValue;
        public uint SpecularSrvIndex = uint.MaxValue;
        public uint BrdfLutSrvIndex = uint.MaxValue;
    }
}
