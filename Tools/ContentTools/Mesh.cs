using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ContentTools
{
    public class Mesh()
    {
        // Initial data
        public List<Vector3> Positions { get; set; } = [];
        public List<Vector3> Normals { get; set; } = [];
        public List<Vector4> Tangents { get; set; } = [];
        public List<Vector3> Colors { get; set; } = [];
        public List<List<Vector2>> UVSets { get; set; } = [];
        public List<int> MaterialIndices { get; set; } = [];
        public int[] MaterialUsed { get=> MaterialIndices.Distinct().ToArray(); } 

        public List<uint> RawIndices { get; set; } = [];

        // Intermediate data
        public List<Vertex> Vertices { get; set; } = [];
        public List<uint> Indices { get; set; } = [];

        // Output data
        public string Name { get; set; }
        public ElementsType ElementsType { get; set; } 
        public byte[] PositionBuffer { get; set; }
        public byte[] ElementBuffer { get; set; }

        public float LODThreshold { get; set; } = -1f;
        public int LODId { get; set; } = -1;
    }
}
