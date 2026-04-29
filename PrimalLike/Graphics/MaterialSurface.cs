
namespace PrimalLike.Graphics
{
    public struct MaterialSurface()
    {
        public Color4 BaseColor = Color4.One;
        public Color3 Emissive = Color3.Zero;
        public byte Metallic = 0;
        public byte Roughness = 255;
        public byte InputMask = 0; // A set bit means to use a texture for the corresponding surface property
        public ushort EmissiveIntensity = 0;
    }
}
