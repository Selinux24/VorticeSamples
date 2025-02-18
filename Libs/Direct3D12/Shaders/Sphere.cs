using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct Sphere
    {
        public Vector3 Center;
        public float Radius;
    }
}
