using System.Numerics;

namespace ContentTools
{
    /// <summary>
    /// Represents a vertex in a mesh.
    /// </summary>
    public class Vertex
    {
        public Vector4 Tangent { get; set; }
        public Vector4 JointWeights { get; set; }
        public uint[] JointIndices { get; set; } = [uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue];
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector2 UV { get; set; }
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
    }
}
