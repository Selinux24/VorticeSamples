using System.Numerics;

namespace ContentTools
{
    /// <summary>
    /// Represents a vertex in a mesh.
    /// </summary>
    public struct Vertex()
    {
        public Vector4 Tangent;
        public Vector4 JointWeights;
        public uint[] JointIndices = [uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue];
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public byte Red;
        public byte Green;
        public byte Blue;
    }
}
