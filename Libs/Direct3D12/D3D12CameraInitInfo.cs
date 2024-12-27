using PrimalLike.Graphics;
using System.Numerics;

namespace Direct3D12
{
    /// <summary>
    /// Camera initialization information
    /// </summary>
    public struct D3D12CameraInitInfo : ICameraInitInfo
    {
        /// <inheritdoc/>
        public int EntityId { get; set; }
        /// <inheritdoc/>
        public bool IsDirty { get; set; }
        /// <inheritdoc/>
        public Vector3 Up { get; set; }
        /// <inheritdoc/>
        public float NearZ { get; set; }
        /// <inheritdoc/>
        public float FarZ { get; set; }
        /// <inheritdoc/>
        public float FieldOfView { get; set; }
        /// <inheritdoc/>
        public float AspectRatio { get; set; }
    }
}
