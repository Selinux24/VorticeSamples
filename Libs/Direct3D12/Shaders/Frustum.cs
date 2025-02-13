using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct Frustum
    {
        public Plane Plane0;
        public Plane Plane1;
        public Plane Plane2;
        public Plane Plane3;
    }
}
