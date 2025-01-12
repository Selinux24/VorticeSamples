
namespace PrimalLike.Graphics
{
    public struct MaterialInitInfo()
    {
        public MaterialTypes Type;
        public int TextureCount; // NOTE: textures are optional, so, texture count may be 0 and texture_ids may be nullptr.
        public IdType[] ShaderIds = new IdType[(uint)ShaderTypes.Count];
        public IdType[] TextureIds;
    }
}
