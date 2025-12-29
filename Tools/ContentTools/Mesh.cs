using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ContentTools
{
    public class Mesh()
    {
        // Initial data
        public Vector3[] Positions = [];
        public Vector3[] Normals = [];
        public Vector4[] Tangents = [];
        public Vector3[] Colors = [];
        public Vector2[][] UVSets = [];
        public int[] MaterialIndices = [];
        public uint[] RawIndices = [];
        public int[] MaterialUsed { get => [.. MaterialIndices.Distinct()]; }

        // Intermediate data
        public List<Vertex> Vertices { get; private set; } = [];
        public List<uint> Indices { get; private set; } = [];

        // Output data
        public string Name { get; set; }
        public ElementsType ElementsType { get; set; }
        public byte[] PositionBuffer { get; set; }
        public byte[] ElementBuffer { get; set; }

        public float LodThreshold { get; set; } = -1f;
        public uint LodId { get; set; } = uint.MaxValue;

        public void PackVertices()
        {
            int numVertices = Vertices.Count;
            Debug.Assert(numVertices > 0);

            int positionsCapacity = Marshal.SizeOf<Vector3>() * numVertices;
            using MemoryStream msPositionBuffer = new(positionsCapacity);
            for (int i = 0; i < numVertices; i++)
            {
                msPositionBuffer.Write(BitConverter.GetBytes(Vertices[i].Position.X));
                msPositionBuffer.Write(BitConverter.GetBytes(Vertices[i].Position.Y));
                msPositionBuffer.Write(BitConverter.GetBytes(Vertices[i].Position.Z));
            }
            PositionBuffer = msPositionBuffer.ToArray();

            var processor = Geometry.Processors()[ElementsType];

            int elementsCapacity = PackingHelper.GetVertexElementsSize(ElementsType) * numVertices;
            using MemoryStream msElementsType = new(elementsCapacity);

            for (int i = 0; i < numVertices; i++)
            {
                processor(msElementsType, Vertices[i]);
            }

            ElementBuffer = msElementsType.ToArray();
        }
    }
}
