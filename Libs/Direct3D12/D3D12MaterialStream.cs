using PrimalLike.Common;
using PrimalLike.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utilities;

namespace Direct3D12
{
    class D3D12MaterialStream
    {
        const uint ShaderFlagsIndex = sizeof(MaterialTypes);
        const uint RootSignatureIndex = ShaderFlagsIndex + sizeof(ShaderFlags);
        const uint TextureCountIndex = RootSignatureIndex + sizeof(uint);

        private readonly IntPtr buffer;

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

        public D3D12MaterialStream(IntPtr materialBuffer)
        {
            buffer = materialBuffer;

            Initialize();
        }
        public D3D12MaterialStream(IntPtr materialBuffer, MaterialInitInfo info)
        {
            Debug.Assert(materialBuffer == IntPtr.Zero);

            int shaderCount = 0;
            int flags = 0;
            for (int i = 0; i < (int)ShaderTypes.Count; i++)
            {
                if (IdDetail.IsValid(info.ShaderIds[i]))
                {
                    shaderCount++;
                    flags |= 1 << i;
                }
            }

            Debug.Assert(shaderCount != 0 && flags != 0);

            int bufferSize =
                sizeof(MaterialTypes) +                             // material type
                sizeof(ShaderFlags) +                               // shader flags
                sizeof(uint) +                                      // root signature id
                sizeof(uint) +                                      // texture count
                sizeof(uint) * shaderCount +                        // shader ids
                (sizeof(uint) + sizeof(uint)) * info.TextureCount;  // texture ids and descriptor indices (maybe 0 if no textures used).

            materialBuffer = Marshal.AllocHGlobal(bufferSize);
            BlobStreamWriter blob = new(materialBuffer, bufferSize);

            blob.Write(info.Type);
            blob.Write(flags);
            blob.Write(D3D12Content.CreateRootSignature(info.Type, (ShaderFlags)flags));
            blob.Write(info.TextureCount);

            buffer = materialBuffer;

            Initialize();

            if (info.TextureCount > 0)
            {
                Array.Copy(info.TextureIds, textureIds, info.TextureCount * sizeof(uint));
                D3D12Content.GetTextureDescriptorIndices(textureIds, descriptorIndices);
            }

            uint shaderIndex = 0;
            for (uint i = 0; i < (uint)ShaderTypes.Count; i++)
            {
                if (IdDetail.IsValid(info.ShaderIds[i]))
                {
                    shaderIds[shaderIndex] = info.ShaderIds[i];
                    shaderIndex++;
                }
            }

            Debug.Assert(shaderIndex == (uint)shaderFlags);
        }

        private void Initialize()
        {
            Debug.Assert(buffer != IntPtr.Zero);

            BlobStreamReader blob = new(buffer);

            type = (MaterialTypes)blob.Read<uint>();
            shaderFlags = (ShaderFlags)blob.Read<uint>();
            rootSignatureId = blob.Read<uint>();
            textureCount = blob.Read<uint>();

            shaderIds = blob.Read<uint>((uint)ShaderTypes.Count);
            //textureIds = textureCount > 0 ? shaderIds[(uint)shaderFlags] : null;
            //descriptorIndices = textureCount > 0 ? textureIds[textureCount] : null;
        }
    }
}
