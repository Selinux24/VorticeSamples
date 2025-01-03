﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ContentTools
{
    /// <summary>
    /// Geometry utils
    /// </summary>
    public static class Geometry
    {
        private const float Epsilon = 1e-5f;

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
        private static void DetermineElementsType(Mesh m)
        {
            if (m.Normals.Count > 0)
            {
                if (m.Normals.Count > 0 && m.UVSets[0].Count > 0)
                {
                    m.ElementsType = ElementsType.StaticNormalTexture;
                }
                else
                {
                    m.ElementsType = ElementsType.StaticNormal;
                }
            }
            else if (m.Normals.Count > 0)
            {
                m.ElementsType = ElementsType.StaticColor;
            }

            // TODO: we lack data for skeletal meshes. Expand for skeletal meshes later.
        }
        private static void ProcessVertices(Mesh m, GeometryImportSettings settings)
        {
            Debug.Assert(m.RawIndices.Count % 3 == 0);
            if (settings.CalculateNormals || m.Normals.Count == 0)
            {
                RecalculateNormals(m);
            }

            ProcessNormals(m, settings.SmoothingAngle);

            if (m.UVSets.Count > 0)
            {
                ProcessUVs(m);
            }

            DetermineElementsType(m);
            PackVertices(m);
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
            float cosAlpha = MathF.Cos(MathF.PI - smoothingAngle * MathF.PI / 180f);
            bool isHardEdge = MathF.Abs(smoothingAngle - 180f) < Epsilon;
            bool isSoftEdge = MathF.Abs(smoothingAngle) < Epsilon;
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
            Debug.Assert(numVertices > 0 && numIndices > 0);

            var idxRef = new List<List<int>>(numVertices);
            for (int i = 0; i < numVertices; ++i)
            {
                idxRef.Add([]);
            }

            for (int i = 0; i < numIndices; ++i)
            {
                m.Indices.Add(0);
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
        private static int GetVertexElementSize(ElementsType type)
        {
            return type switch
            {
                ElementsType.StaticNormal => StaticNormal.GetStride(),
                ElementsType.StaticNormalTexture => StaticNormalTexture.GetStride(),
                ElementsType.StaticColor => StaticColor.GetStride(),
                ElementsType.Skeletal => Skeletal.GetStride(),
                ElementsType.SkeletalColor => SkeletalColor.GetStride(),
                ElementsType.SkeletalNormal => SkeletalNormal.GetStride(),
                ElementsType.SkeletalNormalColor => SkeletalNormalColor.GetStride(),
                ElementsType.SkeletalNormalTexture => SkeletalNormalTexture.GetStride(),
                ElementsType.SkeletalNormalTextureColor => SkeletalNormalTextureColor.GetStride(),
                _ => 0
            };
        }
        private static void PackVertices(Mesh m)
        {
            int numVertices = m.Vertices.Count;
            Debug.Assert(numVertices > 0);

            int positionsCapacity = Marshal.SizeOf(typeof(Vector3)) * numVertices;
            using MemoryStream msPositionBuffer = new(positionsCapacity);
            for (int i = 0; i < numVertices; i++)
            {
                msPositionBuffer.Write(BitConverter.GetBytes(m.Vertices[i].Position.X));
                msPositionBuffer.Write(BitConverter.GetBytes(m.Vertices[i].Position.Y));
                msPositionBuffer.Write(BitConverter.GetBytes(m.Vertices[i].Position.Z));
            }
            m.PositionBuffer = msPositionBuffer.ToArray();

            int elementsCapacity = GetVertexElementSize(m.ElementsType) * numVertices;
            using MemoryStream msElementsType = new(elementsCapacity);

            switch (m.ElementsType)
            {
                case ElementsType.StaticColor:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        StaticColor.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.StaticNormal:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        StaticNormal.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.StaticNormalTexture:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        StaticNormalTexture.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.Skeletal:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        Skeletal.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.SkeletalColor:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        SkeletalColor.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.SkeletalNormal:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        SkeletalNormal.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.SkeletalNormalColor:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        SkeletalNormalColor.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.SkeletalNormalTexture:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        SkeletalNormalTexture.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
                case ElementsType.SkeletalNormalTextureColor:
                {
                    for (int i = 0; i < numVertices; i++)
                    {
                        SkeletalNormalTextureColor.Write(msElementsType, m.Vertices[i]);
                    }
                }
                break;
            }

            m.ElementBuffer = msElementsType.ToArray();
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
                    PackMeshData(buffer, ref at, m);
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
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            foreach (var lod in scene.LODGroups)
            {
                int lodNameLength = lod.Name.Length;

                int lodSize =
                    sizeof(int) +
                    lodNameLength +         // LOD name length and room for LOD name string
                    sizeof(int);            // number of meshes in this LOD

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
            int positionBufferSize = m.PositionBuffer.Length;
            Debug.Assert(positionBufferSize == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            int elementBufferSize = m.ElementBuffer.Length;
            Debug.Assert(elementBufferSize == GetVertexElementSize(m.ElementsType) * numVertices);
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            int indexBufferSize = indexSize * numIndices;

            int size =
                sizeof(int) + nameLength +  // mesh name length and room for mesh name string
                sizeof(int) +               // lod id
                sizeof(int) +               // vertex element size (vertex size excluding position element)
                sizeof(int) +               // element type enumeration
                sizeof(int) +               // number of vertices
                sizeof(int) +               // index size (16 bit or 32 bit)
                sizeof(int) +               // number of indices
                sizeof(float) +             // LOD threshold
                positionBufferSize +        // room for vertex positions
                elementBufferSize +         // room for vertex elements
                indexBufferSize;            // room for indices

            return size;
        }
        private static void PackMeshData(byte[] buffer, ref int at, Mesh m)
        {
            // mesh name
            WriteData(buffer, ref at, m.Name);

            // lod id
            WriteData(buffer, ref at, m.LODId);

            // elements size
            int elementsSize = GetVertexElementSize(m.ElementsType);
            WriteData(buffer, ref at, elementsSize);

            // elements type enumeration
            WriteData(buffer, ref at, (uint)m.ElementsType);

            // number of vertices
            int numVertices = m.Vertices.Count;
            WriteData(buffer, ref at, numVertices);

            // index size (16 bit or 32 bit)
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            WriteData(buffer, ref at, indexSize);

            // number of indices
            int numIndices = m.Indices.Count;
            WriteData(buffer, ref at, numIndices);

            // LOD threshold
            WriteData(buffer, ref at, m.LODThreshold);

            // position buffer
            Debug.Assert(m.PositionBuffer.Length == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            WriteData(buffer, ref at, m.PositionBuffer);

            // element buffer
            Debug.Assert(m.ElementBuffer.Length == elementsSize * numVertices);
            WriteData(buffer, ref at, m.ElementBuffer);

            // index data
            int indexDataLenght = indexSize * numIndices;
            if (indexSize == (uint)sizeof(ushort))
            {
                ushort[] data = m.Indices.Take(numIndices).Select(i => (ushort)i).ToArray();
                WriteData(buffer, ref at, data.ToArray(), indexDataLenght);
            }
            else
            {
                var data = m.Indices.ToArray();
                WriteData(buffer, ref at, data, indexDataLenght);
            }
        }
        private static void WriteData(byte[] buffer, ref int at, string value)
        {
            WriteData(buffer, ref at, BitConverter.GetBytes(value.Length));
            WriteData(buffer, ref at, Encoding.UTF8.GetBytes(value));
        }
        private static void WriteData(byte[] buffer, ref int at, int value)
        {
            WriteData(buffer, ref at, BitConverter.GetBytes(value));
        }
        private static void WriteData(byte[] buffer, ref int at, float value)
        {
            WriteData(buffer, ref at, BitConverter.GetBytes(value));
        }
        private static void WriteData(byte[] buffer, ref int at, ushort[] data, int length)
        {
            Buffer.BlockCopy(data, 0, buffer, at, length);
            at += length;
        }
        private static void WriteData(byte[] buffer, ref int at, uint[] data, int length)
        {
            Buffer.BlockCopy(data, 0, buffer, at, length);
            at += length;
        }
        private static void WriteData(byte[] dst, ref int at, byte[] src)
        {
            Buffer.BlockCopy(src, 0, dst, at, src.Length);
            at += src.Length;
        }
    }
}
