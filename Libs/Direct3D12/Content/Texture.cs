using System;
using System.Diagnostics;
using Utilities;

namespace Direct3D12.Content
{
    static class Texture
    {
        static readonly FreeList<D3D12Texture> textures = new();
        static readonly object textureMutex = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {

        }

        public static uint Add(ref IntPtr data)
        {
            return uint.MaxValue;
        }
        public static void Remove(uint id)
        {

        }
        public static void GetDescriptorIndices(uint[] textureIds, uint[] indices)
        {
            Debug.Assert(textureIds != null && indices != null);
            lock (textureMutex)
            {
                for (uint i = 0; i < textureIds.Length; i++)
                {
                    indices[i] = textures[i].Srv.Index;
                }
            }
        }
    }
}
