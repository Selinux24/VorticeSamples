using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct LightCullingLightInfo
    {
        public Vector3 Position;
        public float Range;

        public Vector3 Direction;
        public float ConeRadius;

        public uint Type;
        public Vector3 _pad;
    }
}
