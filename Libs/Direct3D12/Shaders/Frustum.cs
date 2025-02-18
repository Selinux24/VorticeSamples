using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct Frustum
    {
#if USE_BOUNDING_SPHERES
        public Vector3 ConeDirection;
        public float UnitRadius;
#else
        public Plane Plane0;
        public Plane Plane1;
        public Plane Plane2;
        public Plane Plane3;
#endif
    }
}
