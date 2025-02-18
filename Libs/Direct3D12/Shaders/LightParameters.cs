using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct LightParameters
    {
        public Vector3 Position;
        public float Intensity;

        public Vector3 Direction;
        public float Range;

        public Vector3 Color;
        public float CosUmbra;      // Cosine of the hald angle of umbra

        public Vector3 Attenuation;
        public float CosPenumbra;   // Cosine of the hald angle of penumbra

#if !USE_BOUNDING_SPHERES
        public uint Type;
        public Vector3 _pad;
#endif
    }
}
