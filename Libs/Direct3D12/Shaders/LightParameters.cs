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
        public uint Type;

        public Vector3 Color;
        public float Range;

        public Vector3 Attenuation;
        public float CosUmbra;      // Cosine of the hald angle of umbra

        public float CosPenumbra;   // Cosine of the hald angle of penumbra
        public Vector3 _pad;
    }
}
