using System.Numerics;

namespace Direct3D12.Shaders
{

    struct PerObjectData
    {
        public Matrix4x4 World;
        public Matrix4x4 InvWorld;
        public Matrix4x4 WorldViewProjection;
    }
}
