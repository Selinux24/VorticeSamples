using PrimalLike.Common;

namespace PrimalLike.Graphics
{
    public struct MaterialInitInfo
    {
        public MaterialTypes Type;
        public int TextureCount; // NOTE: textures are optional, so, texture count may be 0 and texture_ids may be nullptr.
        public IdType[] ShaderIds;
        public IdType[] TextureIds;

        public MaterialInitInfo()
        {
            ShaderIds = new IdType[(uint)ShaderTypes.Count];
            for (int i = 0; i < ShaderIds.Length; i++)
            {
                ShaderIds[i] = uint.MaxValue;
            }
        }

        public readonly void GetShaderFlags(out ShaderFlags shaderFlags, out int shaderCount)
        {
            shaderCount = 0;
            shaderFlags = ShaderFlags.None;
            for (int i = 0; i < (int)ShaderTypes.Count; i++)
            {
                if (IdDetail.IsValid(ShaderIds[i]))
                {
                    shaderCount++;
                    shaderFlags |= (ShaderFlags)(1 << i);
                }
            }
        }

        public static void GetShaderFlagsCount(ShaderFlags flags, out int shaderCount)
        {
            shaderCount = 0;
            for (uint i = 0; i < (uint)ShaderTypes.Count; i++)
            {
                var flag = (ShaderFlags)(1 << (int)i);

                if (flags.HasFlag(flag))
                {
                    shaderCount++;
                }
            }
        }
    }
}
