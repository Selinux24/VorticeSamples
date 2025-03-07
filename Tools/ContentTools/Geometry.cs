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
        public class Model(string name)
        {
            public string Name { get; set; } = name ?? $"model_{Guid.NewGuid()}";
            public List<LODGroup> LODGroups { get; set; } = [];
        }
        public class LODGroup()
        {
            public string Name { get; set; }
            public List<Mesh> Meshes { get; set; } = [];
        }
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
            public List<Vertex> Vertices { get; set; } = [];
            public List<uint> Indices { get; set; } = [];

            // Output data
            public string Name { get; set; }
            public ElementsType ElementsType { get; set; }
            public byte[] PositionBuffer { get; set; }
            public byte[] ElementBuffer { get; set; }

            public float LodThreshold { get; set; } = -1f;
            public uint LodId { get; set; } = uint.MaxValue;
        }

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

        public static void ProcessModel(Model model, GeometryImportSettings settings, Progression progression = null)
        {
            foreach (var lod in model.LODGroups)
            {
                foreach (var m in lod.Meshes)
                {
                    ProcessVertices(m, settings);
                }
            }
        }
        private static ElementsType DetermineElementsType(Mesh m)
        {
            ElementsType type = 0;

            if (m.Normals.Length > 0)
            {
                if (m.UVSets.Length > 0 && m.UVSets[0].Length > 0)
                {
                    type = ElementsType.StaticNormalTexture;
                }
                else
                {
                    type = ElementsType.StaticNormal;
                }
            }
            else if (m.Colors.Length > 0)
            {
                type = ElementsType.StaticColor;
            }

            // TODO: we lack data for skeletal meshes. Expand for skeletal meshes later.
            return type;
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

            m.ElementsType = DetermineElementsType(m);
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

                    Vector3 n1 = m.Normals[m.RawIndices[jRef]];
                    if (!isHardEdge)
                    {
                        uint k = j + 1;
                        while (k < numRefs)
                        {
                            int kRef = refs[(int)k];

                            float cosTheta = 0f;
                            Vector3 n2 = m.Normals[m.RawIndices[jRef]];
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

            int elementsCapacity = PackingHelper.GetVertexElementSize(m.ElementsType) * numVertices;
            using MemoryStream msElementsType = new(elementsCapacity);

            for (int i = 0; i < numVertices; i++)
            {
                Processors()[m.ElementsType](msElementsType, m.Vertices[i]);
            }

            m.ElementBuffer = msElementsType.ToArray();
        }

        public static string PackData(Model model, string assetsFolder)
        {
            int sceneSize = GetSceneSize(model);
            IntPtr buffer = Marshal.AllocHGlobal(sceneSize);

            BlobStreamWriter blob = new(buffer, sceneSize);

            // scene name
            blob.Write(model.Name);

            // number of LODs
            blob.Write(model.LODGroups.Count);

            foreach (var lod in model.LODGroups)
            {
                // LOD name
                blob.Write(lod.Name);

                // number of meshes in this LOD
                blob.Write(lod.Meshes.Count);

                foreach (var m in lod.Meshes)
                {
                    PackMesh(m, blob);
                }
            }

            Debug.Assert(sceneSize == blob.Offset);

            string fileName = Path.Combine(assetsFolder, Path.ChangeExtension(model.Name, ".asset"));
            if (!Directory.Exists(assetsFolder))
            {
                Directory.CreateDirectory(assetsFolder);
            }
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            blob.SaveToFile(fileName);

            return fileName;
        }
        private static int GetSceneSize(Model scene)
        {
            int size;
            int sceneNameLength = scene.Name.Length;

            size =
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            foreach (var lod in scene.LODGroups)
            {
                int lodSize;
                int lodNameLength = lod.Name.Length;

                lodSize =
                    sizeof(int) + lodNameLength +   // LOD name length and room for LOD name string
                    sizeof(int);                    // number of meshes in this LOD

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
            Debug.Assert(elementBufferSize == PackingHelper.GetVertexElementSize(m.ElementsType) * numVertices);
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            int indexBufferSize = indexSize * numIndices;

            int size =
                sizeof(int) + nameLength +      // mesh name length and room for mesh name string
                sizeof(uint) +                  // lod id
                sizeof(int) +                   // vertex element size (vertex size excluding position element)
                sizeof(int) +                   // element type enumeration
                sizeof(int) +                   // number of vertices
                sizeof(int) +                   // index size (16 bit or 32 bit)
                sizeof(int) +                   // number of indices
                sizeof(float) +                 // LOD threshold
                positionBufferSize +            // room for vertex positions
                elementBufferSize +             // room for vertex elements
                indexBufferSize;                // room for indices

            return size;
        }
        private static void PackMesh(Mesh m, BlobStreamWriter blob)
        {
            // mesh name
            blob.Write(m.Name);

            // lod id
            blob.Write(m.LodId);

            // elements size
            int elementsSize = PackingHelper.GetVertexElementSize(m.ElementsType);
            blob.Write(elementsSize);

            // elements type enumeration
            blob.Write((uint)m.ElementsType);

            // number of vertices
            int numVertices = m.Vertices.Count;
            blob.Write(numVertices);

            // index size (16 bit or 32 bit)
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            blob.Write(indexSize);

            // number of indices
            int numIndices = m.Indices.Count;
            blob.Write(numIndices);

            // LOD threshold
            blob.Write(m.LodThreshold);

            // position buffer
            Debug.Assert(m.PositionBuffer.Length == Marshal.SizeOf(typeof(Vector3)) * numVertices);
            blob.Write(m.PositionBuffer);

            // element buffer
            Debug.Assert(m.ElementBuffer.Length == elementsSize * numVertices);
            blob.Write(m.ElementBuffer);

            // index data
            int indexDataLenght = indexSize * numIndices;
            if (indexSize == (uint)sizeof(ushort))
            {
                ushort[] data = [.. m.Indices.Select(i => (ushort)i)];
                blob.Write(data);
            }
            else
            {
                uint[] data = [.. m.Indices];
                blob.Write(data);
            }
        }

        private static bool NearEqual(float n1, float n2)
        {
            return MathF.Abs(MathF.Abs(n1) - MathF.Abs(n2)) <= Epsilon;
        }
        private static void AppendToVectorPod<T>(ref T[] dst, T[] src)
        {
            if (src.Length == 0)
            {
                return;
            }

            int numElements = dst.Length;
            Array.Resize(ref dst, dst.Length + src.Length);
            Array.Copy(src, 0, dst, numElements, src.Length);
        }

        public static bool CoalesceMeshes(LODGroup lod, Progression progression, out Mesh combinedMesh)
        {
            Debug.Assert(lod.Meshes.Count > 0);
            var firstMesh = lod.Meshes[0];

            int uvSets = firstMesh.UVSets.Length;
            combinedMesh = new()
            {
                Name = firstMesh.Name,
                ElementsType = DetermineElementsType(firstMesh),
                LodThreshold = firstMesh.LodThreshold,
                LodId = firstMesh.LodId,
                UVSets = new Vector2[uvSets][]
            };

            for (int i = 0; i < uvSets; i++)
            {
                combinedMesh.UVSets[i] = [];
            }

            for (uint meshIdx = 0; meshIdx < lod.Meshes.Count; meshIdx++)
            {
                var m = lod.Meshes[(int)meshIdx];

                if (combinedMesh.ElementsType != DetermineElementsType(m) ||
                    combinedMesh.UVSets.Length != m.UVSets.Length ||
                    combinedMesh.LodId != m.LodId ||
                    !NearEqual(combinedMesh.LodThreshold, m.LodThreshold))
                {
                    combinedMesh = null;

                    return false;
                }

                int positionCount = combinedMesh.Positions.Length;
                int rawIndexBase = combinedMesh.RawIndices.Length;

                AppendToVectorPod(ref combinedMesh.Positions, m.Positions);
                AppendToVectorPod(ref combinedMesh.Normals, m.Normals);
                AppendToVectorPod(ref combinedMesh.Tangents, m.Tangents);
                AppendToVectorPod(ref combinedMesh.Colors, m.Colors);

                for (int i = 0; i < combinedMesh.UVSets.Length; i++)
                {
                    AppendToVectorPod(ref combinedMesh.UVSets[i], m.UVSets[i]);
                }

                AppendToVectorPod(ref combinedMesh.MaterialIndices, m.MaterialIndices);
                AppendToVectorPod(ref combinedMesh.RawIndices, m.RawIndices);

                for (int i = rawIndexBase; i < combinedMesh.RawIndices.Length; i++)
                {
                    combinedMesh.RawIndices[i] += (uint)positionCount;
                }

                progression?.Callback(progression.Value, progression.MaxValue > 1 ? progression.MaxValue - 1 : 1);
            }

            return true;
        }
    }
}
