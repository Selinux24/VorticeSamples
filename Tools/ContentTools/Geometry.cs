using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;

namespace ContentTools
{
    /// <summary>
    /// Geometry utils
    /// </summary>
    public static class Geometry
    {
        private const float Epsilon = 1e-5f;

        private delegate void VertexProcessor(MemoryStream ms, Vertex vertex);
        private static readonly Dictionary<ElementsType, VertexProcessor> processors = [];
        private static Dictionary<ElementsType, VertexProcessor> Processors()
        {
            if (processors.Count > 0)
            {
                return processors;
            }

            processors[ElementsType.StaticColor] = StaticColor.Write;
            processors[ElementsType.StaticNormal] = StaticNormal.Write;
            processors[ElementsType.StaticNormalTexture] = StaticNormalTexture.Write;
            processors[ElementsType.Skeletal] = Skeletal.Write;
            processors[ElementsType.SkeletalColor] = SkeletalColor.Write;
            processors[ElementsType.SkeletalNormal] = SkeletalNormal.Write;
            processors[ElementsType.SkeletalNormalColor] = SkeletalNormalColor.Write;
            processors[ElementsType.SkeletalNormalTexture] = SkeletalNormalTexture.Write;
            processors[ElementsType.SkeletalNormalTextureColor] = SkeletalNormalTextureColor.Write;

            return processors;
        }

        public static void ProcessScene(Scene scene, GeometryImportSettings settings)
        {
            foreach (var lod in scene.LODGroups[0].LODs)
            {
                foreach (var m in lod.Meshes)
                {
                    ProcessVertices(m, settings);
                }
            }
        }
        private static void DetermineElementsType(Mesh m)
        {
            if (m.Normals.Length > 0)
            {
                if (m.UVSets.Length > 0 && m.UVSets[0].Length > 0)
                {
                    m.ElementsType = ElementsType.StaticNormalTexture;
                }
                else
                {
                    m.ElementsType = ElementsType.StaticNormal;
                }
            }
            else if (m.Colors.Length > 0)
            {
                m.ElementsType = ElementsType.StaticColor;
            }

            // TODO: we lack data for skeletal meshes. Expand for skeletal meshes later.
        }
        private static void ProcessVertices(Mesh m, GeometryImportSettings settings)
        {
            Debug.Assert(m.RawIndices.Length % 3 == 0);
            if (settings.CalculateNormals || m.Normals.Length == 0)
            {
                RecalculateNormals(m);
            }

            ProcessNormals(m, settings.SmoothingAngle);

            if (m.UVSets.Length > 0)
            {
                ProcessUVs(m);
            }

            DetermineElementsType(m);
            PackVertices(m);
        }

        private static void RecalculateNormals(Mesh m)
        {
            uint numIndices = (uint)m.RawIndices.LongLength;
            m.Normals = new Vector3[numIndices];

            for (uint i = 0; i < numIndices; i += 3)
            {
                uint i0 = m.RawIndices[i];
                uint i1 = m.RawIndices[i + 1];
                uint i2 = m.RawIndices[i + 2];

                var v0 = m.Positions[i0];
                var v1 = m.Positions[i1];
                var v2 = m.Positions[i2];

                var e0 = v1 - v0;
                var e1 = v2 - v0;
                var n = Vector3.Normalize(Vector3.Cross(e0, e1));

                m.Normals[i0] = n;
                m.Normals[i1] = n;
                m.Normals[i2] = n;
            }
        }

        private static void ProcessNormals(Mesh m, float smoothingAngle)
        {
            int numIndices = m.RawIndices.Length;
            int numVertices = m.Positions.Length;
            Debug.Assert(numIndices > 0 && numVertices > 0);

            float cosAlpha = MathF.Cos(MathF.PI - smoothingAngle * MathF.PI / 180f);
            bool isHardEdge = MathF.Abs(smoothingAngle - 180f) < Epsilon;
            bool isSoftEdge = MathF.Abs(smoothingAngle) < Epsilon;

            m.Indices.Clear();
            m.Indices.AddRange(new uint[numIndices]);

            var idxRef = GetMeshRawIdRefList(m);

            for (uint i = 0; i < numVertices; i++)
            {
                var refs = idxRef[i];
                uint numRefs = (uint)refs.Count;

                for (uint j = 0; j < numRefs; j++)
                {
                    int jRef = refs[(int)j];

                    m.Indices[jRef] = (uint)m.Vertices.Count;
                    Vertex v = new()
                    {
                        Position = m.Positions[m.RawIndices[jRef]]
                    };

                    Vector3 n1 = m.Normals[jRef];
                    if (!isHardEdge)
                    {
                        uint k = j + 1;
                        while (k < numRefs)
                        {
                            int kRef = refs[(int)k];

                            float cosTheta = 0f;
                            Vector3 n2 = m.Normals[kRef];
                            if (!isSoftEdge)
                            {
                                cosTheta = Vector3.Dot(n1, n2) / n1.Length();
                            }

                            if (isSoftEdge || cosTheta >= cosAlpha)
                            {
                                n1 += n2;

                                m.Indices[kRef] = m.Indices[jRef];
                                refs.RemoveAt((int)k);
                                numRefs--;
                                k--;
                            }

                            k++;
                        }
                    }
                    v.Normal = Vector3.Normalize(n1);

                    m.Vertices.Add(v);
                }
            }
        }
        private static List<int>[] GetMeshRawIdRefList(Mesh m)
        {
            List<List<int>> idxRef = [];

            int numVertices = m.Positions.Length;
            for (int i = 0; i < numVertices; i++)
            {
                idxRef.Add([]);
            }

            int numIndices = m.RawIndices.Length;
            for (int i = 0; i < numIndices; i++)
            {
                idxRef[(int)m.RawIndices[i]].Add(i);
            }

            return [.. idxRef];
        }

        private static void ProcessUVs(Mesh m)
        {
            uint numVertices = (uint)m.Vertices.Count;
            uint numIndices = (uint)m.Indices.Count;
            Debug.Assert(numVertices > 0 && numIndices > 0);
            Debug.Assert(m.Indices.Max() == m.Vertices.Count - 1);

            var idxRef = GetMeshIdRefList(m);

            Vertex[] oldVertices = [.. m.Vertices];
            m.Vertices.Clear();
            uint[] oldIndices = [.. m.Indices];
            m.Indices.Clear();
            m.Indices.AddRange(new uint[numIndices]);
            Debug.Assert(oldIndices.Max() == oldVertices.Length - 1);
            uint firstIndex = oldIndices.Min();

            for (uint i = 0; i < numVertices; i++)
            {
                var refs = idxRef[i];
                uint numRefs = (uint)refs.Count;

                for (uint j = 0; j < numRefs; j++)
                {
                    int jRef = refs[(int)j];

                    m.Indices[jRef] = (uint)m.Vertices.Count;
                    var v = oldVertices[oldIndices[jRef]];
                    v.UV = m.UVSets[0][oldIndices[jRef] - firstIndex];
                    m.Vertices.Add(v);

                    uint k = j + 1;
                    while (k < numRefs)
                    {
                        int kRef = refs[(int)k];

                        var uv1 = m.UVSets[0][oldIndices[kRef] - firstIndex];
                        if (Math.Abs(v.UV.X - uv1.X) < float.Epsilon &&
                            Math.Abs(v.UV.Y - uv1.Y) < float.Epsilon)
                        {
                            m.Indices[kRef] = m.Indices[jRef];
                            refs.RemoveAt((int)k);
                            numRefs--;
                            k--;
                        }

                        k++;
                    }
                }
            }
        }
        private static List<int>[] GetMeshIdRefList(Mesh m)
        {
            List<List<int>> idxRef = [];

            int numVertices = m.Vertices.Count;
            for (int i = 0; i < numVertices; i++)
            {
                idxRef.Add([]);
            }

            int numIndices = m.Indices.Count;
            for (int i = 0; i < numIndices; i++)
            {
                idxRef[(int)m.Indices[i]].Add(i);
            }

            return [.. idxRef];
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

            for (int i = 0; i < numVertices; i++)
            {
                Processors()[m.ElementsType](msElementsType, m.Vertices[i]);
            }

            m.ElementBuffer = msElementsType.ToArray();
        }

        public static void PackForEditor(Scene scene, SceneData data)
        {
            int sceneSize = GetSceneSizeForEditor(scene);
            data.Buffer = Marshal.AllocHGlobal(sceneSize);
            data.BufferSize = sceneSize;

            BlobStreamWriter blob = new(data.Buffer, data.BufferSize);

            // scene name
            blob.Write(scene.Name);

            // number of LODs
            blob.Write(scene.LODGroups[0].LODs.Count);

            foreach (var lod in scene.LODGroups[0].LODs)
            {
                // LOD name
                blob.Write(lod.Name);

                // threshols
                blob.Write(lod.Threshold);

                // number of meshes in this LOD
                blob.Write(lod.Meshes.Count);

                var sizeOfSubmeshesPosition = blob.Position;
                blob.Write(0);

                foreach (var m in lod.Meshes)
                {
                    PackMeshForEditor(m, blob);
                }

                var endOfSubmeshes = blob.Position;
                var sizeOfSubmeshes = (int)(endOfSubmeshes - sizeOfSubmeshesPosition - sizeof(int));

                blob.Position = sizeOfSubmeshesPosition;
                blob.Write(sizeOfSubmeshes);
                blob.Position = endOfSubmeshes;
            }

            Debug.Assert(sceneSize == blob.Offset);
        }
        private static int GetSceneSizeForEditor(Scene scene)
        {
            int size;
            int sceneNameLength = scene.Name.Length;

            size =
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            foreach (var lod in scene.LODGroups[0].LODs)
            {
                int lodSize;
                int lodNameLength = lod.Name.Length;

                lodSize =
                    sizeof(int) +
                    lodNameLength +         // LOD name length and room for LOD name string
                    sizeof(float) +         // LOD threshold
                    sizeof(int) +           // number of meshes in this LOD
                    sizeof(int);            // size of submeshes

                foreach (var m in lod.Meshes)
                {
                    lodSize += GetMeshSizeForEditor(m);
                }

                size += lodSize;
            }

            return size;
        }
        private static int GetMeshSizeForEditor(Mesh m)
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
                sizeof(int) + nameLength +      // mesh name length and room for mesh name string
                sizeof(int) +                   // vertex element size (vertex size excluding position element)
                sizeof(int) +                   // number of vertices
                sizeof(int) +                   // number of indices
                sizeof(int) +                   // element type enumeration
                sizeof(int) +                   // primitive topology
                positionBufferSize +            // room for vertex positions
                elementBufferSize +             // room for vertex elements
                indexBufferSize;                // room for indices

            return size;
        }
        private static void PackMeshForEditor(Mesh m, BlobStreamWriter blob)
        {
            // mesh name
            blob.Write(m.Name);

            // elements size
            int elementsSize = GetVertexElementSize(m.ElementsType);
            blob.Write(elementsSize);

            // number of vertices
            int numVertices = m.Vertices.Count;
            blob.Write(numVertices);

            // number of indices
            int numIndices = m.Indices.Count;
            blob.Write(numIndices);

            // elements type enumeration
            blob.Write((uint)m.ElementsType);

            // primitive topology
            blob.Write((uint)m.PrimitiveTopology);

            // position buffer
            Debug.Assert(m.PositionBuffer.Length == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            blob.Write(m.PositionBuffer);

            // element buffer
            Debug.Assert(m.ElementBuffer.Length == elementsSize * numVertices);
            blob.Write(m.ElementBuffer);

            // index size (16 bit or 32 bit)
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);

            // index data
            int indexDataLenght = indexSize * numIndices;
            if (indexSize == (uint)sizeof(ushort))
            {
                ushort[] data = m.Indices.Take(numIndices).Select(i => (ushort)i).ToArray();
                blob.Write(data);
            }
            else
            {
                var data = m.Indices.ToArray();
                blob.Write(data);
            }
        }

        public static void PackForEngine(Scene scene, SceneData data)
        {
            int sceneSize = GetSceneSizeForEngine(scene);
            data.Buffer = Marshal.AllocHGlobal(sceneSize);
            data.BufferSize = sceneSize;

            BlobStreamWriter blob = new(data.Buffer, data.BufferSize);

            // number of LODs
            blob.Write(scene.LODGroups[0].LODs.Count);

            foreach (var lod in scene.LODGroups[0].LODs)
            {
                // threshols
                blob.Write(lod.Threshold);

                // number of meshes in this LOD
                blob.Write(lod.Meshes.Count);

                var sizeOfSubmeshesPosition = blob.Position;
                blob.Write(0);

                foreach (var m in lod.Meshes)
                {
                    PackMeshForEngine(m, blob);
                }

                var endOfSubmeshes = blob.Position;
                var sizeOfSubmeshes = (int)(endOfSubmeshes - sizeOfSubmeshesPosition - sizeof(int));

                blob.Position = sizeOfSubmeshesPosition;
                blob.Write(sizeOfSubmeshes);
                blob.Position = endOfSubmeshes;
            }

            Debug.Assert(sceneSize == blob.Offset);
        }
        private static int GetSceneSizeForEngine(Scene scene)
        {
            int size = sizeof(int); // number of LODs

            foreach (var lod in scene.LODGroups[0].LODs)
            {
                int lodSize =
                        sizeof(float) +         // LOD threshold
                        sizeof(int) +           // number of meshes in this LOD
                        sizeof(int);            // size of submeshes

                foreach (var m in lod.Meshes)
                {
                    lodSize += GetMeshSizeForEngine(m);
                }

                size += lodSize;
            }

            return size;
        }
        private static int GetMeshSizeForEngine(Mesh m)
        {
            int numVertices = m.Vertices.Count;
            int numIndices = m.Indices.Count;
            int positionBufferSize = m.PositionBuffer.Length;
            Debug.Assert(positionBufferSize == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            int elementBufferSize = m.ElementBuffer.Length;
            Debug.Assert(elementBufferSize == GetVertexElementSize(m.ElementsType) * numVertices);
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            int indexBufferSize = indexSize * numIndices;

            int size =
                sizeof(int) +                               // vertex element size (vertex size excluding position element)
                sizeof(int) +                               // number of vertices
                sizeof(int) +                               // number of indices
                sizeof(int) +                               // element type enumeration
                sizeof(int) +                               // primitive topology
                positionBufferSize +                        // room for vertex positions
                elementBufferSize +                         // room for vertex elements
                indexBufferSize;                            // room for indices

            return size;
        }
        private static void PackMeshForEngine(Mesh m, BlobStreamWriter blob)
        {
            // elements size
            int elementsSize = GetVertexElementSize(m.ElementsType);
            blob.Write(elementsSize);

            // number of vertices
            int numVertices = m.Vertices.Count;
            blob.Write(numVertices);

            // number of indices
            int numIndices = m.Indices.Count;
            blob.Write(numIndices);

            // elements type enumeration
            blob.Write((uint)m.ElementsType);

            // primitive topology
            blob.Write((uint)m.PrimitiveTopology);

            // position buffer
            Debug.Assert(m.PositionBuffer.Length == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            blob.Write(m.PositionBuffer);

            // element buffer
            Debug.Assert(m.ElementBuffer.Length == elementsSize * numVertices);
            blob.Write(m.ElementBuffer);

            // index size (16 bit or 32 bit)
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);

            // index data
            int indexDataLenght = indexSize * numIndices;
            if (indexSize == (uint)sizeof(ushort))
            {
                ushort[] data = m.Indices.Take(numIndices).Select(i => (ushort)i).ToArray();
                blob.Write(data);
            }
            else
            {
                var data = m.Indices.ToArray();
                blob.Write(data);
            }
        }

    }
}
