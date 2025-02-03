using System.Numerics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct GlobalShaderData
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public Matrix4x4 InvProjection;
        public Matrix4x4 ViewProjection;
        public Matrix4x4 InvViewProjection;

        public Vector3 CameraPosition;
        public float ViewWidth;

        public Vector3 CameraDirection;
        public float ViewHeight;

        public uint NumDirectionalLights;
        public float DeltaTime;
    }
}
