using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct Plane
    {
        public Vector3 Normal;
        public float Distance;
    }
}
