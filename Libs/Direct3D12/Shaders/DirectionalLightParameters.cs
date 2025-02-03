using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct DirectionalLightParameters
    {
        public Vector3 Direction;
        public float Intensity;

        public Vector3 Color;
        public float Padding;
    }
}
