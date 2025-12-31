using System.Numerics;

namespace PrimalLike.Graphics
{
    public struct MaterialSurface()
    {
        public Vector4 BaseColor = Vector4.One;
        public Vector3 Emissive = Vector3.Zero;
        public float EmissiveIntensity = 1.0f;
        public float Metallic = 0.0f;
        public float Roughness = 1.0f;
    }
}
