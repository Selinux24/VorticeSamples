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
#if USE_BOUNDING_SPHERES
        // If this is set to -1 then the light is a point light.
        public float CosPenumbra;
#else
        public float ConeRadius;

        public uint Type;
        public Vector3 _pad;
#endif
    }
}
