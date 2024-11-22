using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace ContentTools
{
    static class Geometry
    {
        private static readonly float epsilon = 1e-5f;

        private static void RecalculateNormals(Mesh m)
        {
            int numIndices = m.RawIndices.Count;
            m.Normals.Capacity = numIndices;

            for (int i = 0; i < numIndices; ++i)
            {
                int i0 = m.RawIndices[i];
                int i1 = m.RawIndices[++i];
                int i2 = m.RawIndices[++i];

                Vector3 v0 = m.Positions[i0];
                Vector3 v1 = m.Positions[i1];
                Vector3 v2 = m.Positions[i2];

                Vector3 e0 = v1 - v0;
                Vector3 e1 = v2 - v0;
                Vector3 n = Vector3.Normalize(Vector3.Cross(e0, e1));

                m.Normals[i] = n;
                m.Normals[i - 1] = n;
                m.Normals[i - 2] = n;
            }
        }

        private static void ProcessNormals(Mesh m, float smoothingAngle)
        {
            float cosAlpha = (float)Math.Cos(MathF.PI - smoothingAngle * MathF.PI / 180f);
            bool isHardEdge = Math.Abs(smoothingAngle - 180f) < epsilon;
            bool isSoftEdge = Math.Abs(smoothingAngle) < epsilon;
            int numIndices = m.RawIndices.Count;
            int numVertices = m.Positions.Count;
            Debug.Assert(numIndices > 0 && numVertices > 0);

            m.Indices = new List<int>(new int[numIndices]);

            List<List<int>> idxRef = new List<List<int>>(numVertices);
            for (int i = 0; i < numVertices; ++i)
                idxRef.Add(new List<int>());

            for (int i = 0; i < numIndices; ++i)
                idxRef[m.RawIndices[i]].Add(i);

            for (int i = 0; i < numVertices; ++i)
            {
                var refs = idxRef[i];
                int numRefs = refs.Count;
                for (int j = 0; j < numRefs; ++j)
                {
                    m.Indices[refs[j]] = m.Vertices.Count;
                    Vertex v = new Vertex { Position = m.Positions[m.RawIndices[refs[j]]] };
                    m.Vertices.Add(v);

                    Vector3 n1 = m.Normals[refs[j]];
                    if (!isHardEdge)
                    {
                        for (int k = j + 1; k < numRefs; ++k)
                        {
                            float cosTheta = 0f;
                            Vector3 n2 = m.Normals[refs[k]];
                            if (!isSoftEdge)
                            {
                                cosTheta = Vector3.Dot(n1, n2) / n1.Length();
                            }

                            if (isSoftEdge || cosTheta >= cosAlpha)
                            {
                                n1 += n2;

                                m.Indices[refs[k]] = m.Indices[refs[j]];
                                refs.RemoveAt(k);
                                --numRefs;
                                --k;
                            }
                        }
                    }
                    v.Normal = Vector3.Normalize(n1);
                }
            }
        }

        private static void ProcessUVs(Mesh m)
        {
            var oldVertices = new List<Vertex>(m.Vertices);
            m.Vertices.Clear();
            var oldIndices = new List<int>(m.Indices);
            m.Indices.Clear();

            int numVertices = oldVertices.Count;
            int numIndices = oldIndices.Count;
            if (numVertices == 0 || numIndices == 0) throw new InvalidOperationException();

            var idxRef = new List<List<int>>(numVertices);
            for (int i = 0; i < numVertices; ++i)
            {
                idxRef.Add([]);
            }

            for (int i = 0; i < numIndices; ++i)
            {
                idxRef[oldIndices[i]].Add(i);
            }

            for (int i = 0; i < numIndices; ++i)
            {
                var refs = idxRef[i];
                int numRefs = refs.Count;
                for (int j = 0; j < numRefs; ++j)
                {
                    m.Indices[refs[j]] = m.Vertices.Count;
                    var v = oldVertices[oldIndices[refs[j]]];
                    v.UV = m.UVSets[0][refs[j]];
                    m.Vertices.Add(v);

                    for (int k = j + 1; k < numRefs; ++k)
                    {
                        var uv1 = m.UVSets[0][refs[k]];
                        if (Math.Abs(v.UV.X - uv1.X) < float.Epsilon &&
                            Math.Abs(v.UV.Y - uv1.Y) < float.Epsilon)
                        {
                            m.Indices[refs[k]] = m.Indices[refs[j]];
                            refs.RemoveAt(k);
                            --numRefs;
                            --k;
                        }
                    }
                }
            }
        }

        private static void PackVerticesStatic(Mesh m)
        {
            int numVertices = m.Vertices.Count;
            if (numVertices == 0) throw new InvalidOperationException();
            m.PackedVerticesStatic = new List<PackedVertex>(numVertices);

            for (int i = 0; i < numVertices; ++i)
            {
                var v = m.Vertices[i];
                byte signs = (byte)((v.Normal.Z > 0f) ? 2 : 0);
                ushort normalX = PackFloat16(v.Normal.X, -1f, 1f);
                ushort normalY = PackFloat16(v.Normal.Y, -1f, 1f);

                m.PackedVerticesStatic.Add(new PackedVertex
                {
                    Position = v.Position,
                    Signs = signs,
                    NormalX = normalX,
                    NormalY = normalY,
                    UV = v.UV
                });
            }
        }

        private static void ProcessVertices(Mesh m, GeometryImportSettings settings)
        {
            if (m.RawIndices.Count % 3 != 0) throw new InvalidOperationException();
            if (settings.CalculateNormals || m.Normals.Count == 0)
            {
                RecalculateNormals(m);
            }

            ProcessNormals(m, settings.SmoothingAngle);

            if (m.UVSets.Count > 0)
            {
                ProcessUVs(m);
            }

            PackVerticesStatic(m);
        }

        public static void ProcessScene(Scene scene, GeometryImportSettings settings)
        {
            foreach (var lod in scene.LODGroups)
            {
                foreach (var m in lod.Meshes)
                {
                    ProcessVertices(m, settings);
                }
            }
        }

        public static void PackData(Scene scene, SceneData data)
        {
        }

        private static ushort PackFloat16(float value, float min, float max)
        {
            return (ushort)((value - min) / (max - min) * 65535f);
        }
    }
}
