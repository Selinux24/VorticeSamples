using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct PerObjectData
    {
        public Matrix4x4 World;
        public Matrix4x4 InvWorld;
        public Matrix4x4 WorldViewProjection;

        public Vector4 BaseColor;
        public Vector3 Emissive;
        public float EmissiveIntensity;
        public float AmbientOcclusion;
        public float Metallic;
        public float Roughness;
    }
}
