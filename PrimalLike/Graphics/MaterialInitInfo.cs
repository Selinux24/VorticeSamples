using PrimalLike.Common;
using System;

namespace PrimalLike.Graphics
{
    public struct MaterialInitInfo
    {
        public MaterialSurface Surface = new();
        public MaterialTypes Type = MaterialTypes.Opaque;
        public IdType[] ShaderIds = new IdType[(uint)ShaderTypes.Count];
        public IdType[] TextureIds = null;
        public int TextureCount = 0; // NOTE: textures are optional, so, texture count may be 0 and texture_ids may be nullptr.

        public MaterialInitInfo()
        {
            Array.Fill(ShaderIds, uint.MaxValue);
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
