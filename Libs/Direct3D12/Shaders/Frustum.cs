using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct Frustum
    {
        public Vector3 ConeDirection;
        public float UnitRadius;
    }
}
