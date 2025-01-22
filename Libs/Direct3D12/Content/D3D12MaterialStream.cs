using PrimalLike.Common;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utilities;

namespace Direct3D12.Content
{
    class D3D12MaterialStream
    {
        private readonly nint buffer;

        private uint[] textureIds;
        private uint[] descriptorIndices;
        private uint[] shaderIds;
        private uint rootSignatureId;
        private uint textureCount;
        private MaterialTypes type;
        private ShaderFlags shaderFlags;

        public uint TextureCount { get => textureCount; }
        public MaterialTypes MaterialType { get => type; }
        public ShaderFlags ShaderFlags { get => shaderFlags; }
        public uint RootSignatureId { get => rootSignatureId; }
        public uint[] TextureIds { get => textureIds; }
        public uint[] DescriptorIndices { get => descriptorIndices; }
        public uint[] ShaderIds { get => shaderIds; }

        public D3D12MaterialStream(nint materialBuffer)
        {
            buffer = materialBuffer;

            Initialize();
        }
        public D3D12MaterialStream(ref nint materialBuffer, MaterialInitInfo info)
        {
            Debug.Assert(materialBuffer == nint.Zero);

            info.GetShaderFlags(out ShaderFlags shaderFlags, out int shaderCount);
            Debug.Assert(shaderCount != 0 && shaderFlags != 0);

            int bufferSize =
                sizeof(MaterialTypes) +                             // material type
                sizeof(ShaderFlags) +                               // shader flags
                sizeof(uint) +                                      // root signature id
                sizeof(uint) +                                      // texture count
                sizeof(uint) * shaderCount +                        // shader ids
                (sizeof(uint) + sizeof(uint)) * info.TextureCount;  // texture ids and descriptor indices (maybe 0 if no textures used).

            buffer = materialBuffer = Marshal.AllocHGlobal(bufferSize);
            BlobStreamWriter blob = new(buffer, bufferSize);

            blob.Write((uint)info.Type);
            blob.Write((uint)shaderFlags);
            blob.Write(Material.CreateRootSignature(info.Type, shaderFlags));
            blob.Write(info.TextureCount);

            if (info.TextureCount > 0)
            {
                blob.Write(info.TextureIds);
                Texture.GetDescriptorIndices(textureIds, descriptorIndices);
                blob.Write(descriptorIndices);
            }

            uint shaderIndex = 0;
            for (uint i = 0; i < (uint)ShaderTypes.Count; i++)
            {
                if (IdDetail.IsValid(info.ShaderIds[i]))
                {
                    blob.Write(info.ShaderIds[i]);
                    shaderIndex++;
                }
            }

            Debug.Assert(shaderIndex == shaderCount);

            Initialize();
        }

        private void Initialize()
        {
            Debug.Assert(buffer != nint.Zero);

            BlobStreamReader blob = new(buffer);

            type = (MaterialTypes)blob.Read<uint>();
            shaderFlags = (ShaderFlags)blob.Read<uint>();
            rootSignatureId = blob.Read<uint>();
            textureCount = blob.Read<uint>();

            //Get the number of flags set in shaderFlags
            MaterialInitInfo.GetShaderFlagsCount(shaderFlags, out int shaderCount);

            textureIds = textureCount > 0 ? blob.Read<uint>((int)textureCount) : null;
            descriptorIndices = textureCount > 0 ? blob.Read<uint>((int)textureCount) : null;
            shaderIds = blob.Read<uint>(shaderCount);
        }
    }
}
