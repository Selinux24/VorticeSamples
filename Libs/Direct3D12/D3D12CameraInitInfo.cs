using Engine.Graphics;
using System.Numerics;

namespace Direct3D12
{
    public struct D3D12CameraInitInfo : ICameraInitInfo
    {
        public int EntityId { get; set; }
        public bool IsDirty { get; set; }
        public Vector3 Up { get; set; }
        public float NearZ { get; set; }
        public float FarZ { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
    }
}
