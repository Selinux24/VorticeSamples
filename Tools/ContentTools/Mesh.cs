using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ContentTools
{
    public class Mesh()
    {
        // Initial data
        public Vector3[] Positions { get; set; } = [];
        public Vector3[] Normals { get; set; } = [];
        public Vector4[] Tangents { get; set; } = [];
        public Vector3[] Colors { get; set; } = [];
        public Vector2[][] UVSets { get; set; } = [];
        public int[] MaterialIndices { get; set; } = [];
        public int[] MaterialUsed { get => MaterialIndices.Distinct().ToArray(); }

        public uint[] RawIndices { get; set; } = [];

        // Intermediate data
        public List<Vertex> Vertices { get; set; } = [];
        public List<uint> Indices { get; set; } = [];

        // Output data
        public string Name { get; set; }
        public ElementsType ElementsType { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
        public byte[] PositionBuffer { get; set; }
        public byte[] ElementBuffer { get; set; }
    }
}
