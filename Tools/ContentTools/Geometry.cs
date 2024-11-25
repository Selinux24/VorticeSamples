using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ContentTools
{
    static class Geometry
    {
        private static readonly float epsilon = 1e-5f;

        private static readonly int SizeUSHORT = Marshal.SizeOf(typeof(ushort));
        private static readonly int SizeINT = Marshal.SizeOf(typeof(int));
        private static readonly int SizeUINT = Marshal.SizeOf(typeof(uint));
        private static readonly int SizeFLOAT = Marshal.SizeOf(typeof(float));
        private static readonly int SizePACKEDVERTEX = Marshal.SizeOf(typeof(PackedVertex));

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
        private static void RecalculateNormals(Mesh m)
        {
            int numIndices = m.RawIndices.Count;
            m.Normals.Capacity = numIndices;

            for (int i = 0; i < numIndices; ++i)
            {
                uint i0 = m.RawIndices[i];
                uint i1 = m.RawIndices[++i];
                uint i2 = m.RawIndices[++i];

                Vector3 v0 = m.Positions[(int)i0];
                Vector3 v1 = m.Positions[(int)i1];
                Vector3 v2 = m.Positions[(int)i2];

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

            m.Indices = new(new uint[numIndices]);

            List<List<int>> idxRef = new(numVertices);
            for (int i = 0; i < numVertices; ++i)
            {
                idxRef.Add([]);
            }

            for (int i = 0; i < numIndices; ++i)
            {
                idxRef[(int)m.RawIndices[i]].Add(i);
            }

            for (int i = 0; i < numVertices; ++i)
            {
                var refs = idxRef[i];
                int numRefs = refs.Count;
                for (int j = 0; j < numRefs; ++j)
                {
                    m.Indices[refs[j]] = (uint)m.Vertices.Count;
                    Vertex v = new()
                    {
                        Position = m.Positions[(int)m.RawIndices[refs[j]]]
                    };
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
            var oldIndices = new List<uint>(m.Indices);
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
                idxRef[(int)oldIndices[i]].Add(i);
            }

            for (int i = 0; i < numVertices; ++i)
            {
                var refs = idxRef[i];
                int numRefs = refs.Count;
                for (int j = 0; j < numRefs; ++j)
                {
                    m.Indices[refs[j]] = (uint)m.Vertices.Count;
                    var v = oldVertices[(int)oldIndices[refs[j]]];
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
        private static ushort PackFloat16(float value, float min, float max)
        {
            return (ushort)((value - min) / (max - min) * 65535f);
        }

        public static void PackData(Scene scene, SceneData data)
        {
            int sceneSize = GetSceneSize(scene);
            var buffer = new byte[sceneSize];
            int at = 0;

            // scene name
            WriteData(buffer, ref at, scene.Name);

            // number of LODs
            WriteData(buffer, ref at, scene.LODGroups.Count);

            foreach (var lod in scene.LODGroups)
            {
                // LOD name
                WriteData(buffer, ref at, lod.Name);

                // number of meshes in this LOD
                WriteData(buffer, ref at, lod.Meshes.Count);

                foreach (var m in lod.Meshes)
                {
                    PackMeshData(m, buffer, ref at);
                }
            }

            Debug.Assert(sceneSize == at);

            data.BufferSize = sceneSize;
            data.Buffer = buffer;
        }
        private static int GetSceneSize(Scene scene)
        {
            int sceneNameLength = scene.Name.Length;

            int size =
                SizeINT +                 // name length
                sceneNameLength +         // room for scene name string
                SizeINT;                  // number of LODs

            foreach (var lod in scene.LODGroups)
            {
                int lodNameLength = lod.Name.Length;

                int lodSize =
                    SizeINT +
                    lodNameLength +       // LOD name length and room for LOD name string
                    SizeINT;              // number of meshes in this LOD

                foreach (var m in lod.Meshes)
                {
                    lodSize += GetMeshSize(m);
                }

                size += lodSize;
            }

            return size;
        }
        private static int GetMeshSize(Mesh m)
        {
            int nameLength = m.Name.Length;
            int numVertices = m.Vertices.Count;
            int numIndices = m.Indices.Count;
            int vertexBufferSize = SizePACKEDVERTEX * numVertices;
            int indexSize = (numVertices < (1 << 16)) ? SizeUSHORT : SizeUINT;
            int indexBufferSize = indexSize * numIndices;

            int size =
                SizeINT + nameLength +       // mesh name length and room for mesh name string
                SizeINT +                    // lod id
                SizeINT +                    // vertex size
                SizeINT +                    // number of vertices
                SizeINT +                    // index size (16 bit or 32 bit)
                SizeINT +                    // number of indices
                SizeFLOAT +                  // LOD threshold
                vertexBufferSize +           // room for vertices
                indexBufferSize;             // room for indices

            return size;
        }
        private static void PackMeshData(Mesh m, byte[] buffer, ref int at)
        {
            // mesh name
            WriteData(buffer, ref at, m.Name);

            // lod id
            WriteData(buffer, ref at, m.LODId);

            int vertexSize = SizePACKEDVERTEX;
            int numVertices = m.Vertices.Count;
            int indexSize = (numVertices < (1 << 16)) ? SizeUSHORT : SizeUINT;
            int numIndices = m.Indices.Count;
            int vertexDataLength = vertexSize * numVertices;

            // vertex size
            WriteData(buffer, ref at, vertexSize);

            // number of vertices
            WriteData(buffer, ref at, numVertices);

            // index size (16 bit or 32 bit)
            WriteData(buffer, ref at, indexSize);

            // number of indices
            WriteData(buffer, ref at, numIndices);

            // LOD threshold
            WriteData(buffer, ref at, m.LODThreshold);

            // vertex data
            WriteData(buffer, ref at, vertexDataLength);

            // index data
            int indexDataLenght = indexSize * numIndices;
            var data = m.Indices.ToArray();
            var indices = new List<uint>(numIndices);
            if (indexSize == (uint)SizeUSHORT)
            {
                for (uint i = 0; i < numIndices; ++i)
                {
                    indices.Add(m.Indices[(int)i]);
                }
                data = [.. indices];
            }
            WriteData(buffer, ref at, data, indexDataLenght);
        }
        private static void WriteData(byte[] buffer, ref int at, string value)
        {
            int valueLength = value.Length;
            Buffer.BlockCopy(BitConverter.GetBytes(valueLength), 0, buffer, at, SizeINT);
            at += SizeINT;
            Buffer.BlockCopy(Encoding.UTF8.GetBytes(value), 0, buffer, at, valueLength);
            at += valueLength;
        }
        private static void WriteData(byte[] buffer, ref int at, int value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, at, SizeINT);
            at += SizeINT;
        }
        private static void WriteData(byte[] buffer, ref int at, float value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, at, SizeFLOAT);
            at += SizeFLOAT;
        }
        private static void WriteData(byte[] buffer, ref int at, uint[] data, int length)
        {
            Buffer.BlockCopy(data, 0, buffer, at, length);
            at += length;
        }
    }
}
