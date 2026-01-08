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
        public float CosPenumbra; // If this is set to -1 then the light is a point light.
    }
}
