using ContentTools.MikkTSpace;
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
        #region Classes

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
        class MeshMikkTSpace(Mesh m) : MikkTSpaceGenerator(m)
        {
            public static void CalculateMikkTSpace(Mesh m)
            {
                // Don't use any imported tangents
                m.Tangents = [];

                MeshMikkTSpace mikkTSpace = new(m);
                mikkTSpace.GenTangSpace();
            }

            public override int GetNumFaces()
            {
                var m = GetMesh<Mesh>();
                return m.Indices.Count / 3;
            }
            public override int GetNumVerticesOfFace(int faceIndex)
            {
                return 3;
            }
            public override Vector3 GetPosition(int faceIndex, int vertIndex)
            {
                var m = GetMesh<Mesh>();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].Position;
            }
            public override Vector3 GetNormal(int faceIndex, int vertIndex)
            {
                var m = GetMesh<Mesh>();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].Normal;
            }
            public override Vector2 GetTexCoord(int faceIndex, int vertIndex)
            {
                var m = GetMesh<Mesh>();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].UV;
            }
            public override void SetTSpaceBasic(Vector3 tangent, float sign, int faceIndex, int vertIndex)
            {
                var m = GetMesh<Mesh>();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                m.Vertices[(int)index].Tangent = new Vector4(tangent, sign);
            }
        }

        #endregion

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
            uint totalMeshes = (uint)model.LODGroups.Sum(lod => lod.Meshes.Count);
            uint currentProgress = 0;

            foreach (var lod in model.LODGroups)
            {
                foreach (var m in lod.Meshes)
                {
                    ProcessVertices(m, settings);

                    progression?.Callback(++currentProgress, totalMeshes);
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

            if ((settings.CalculateTangents || m.Tangents.Length == 0) && m.UVSets.Length > 0)
            {
                MeshMikkTSpace.CalculateMikkTSpace(m);
                //CalculateTangents(m);
            }

            // NOTE: m.tangents contains values of the imported tangent vectors. It will be empty
            //       if tangents where calculated. Therefore, process_tangents is only called
            //       when tangents are imported from the source file.
            if (m.Tangents.Length == 0)
            {
                ProcessTangents(m);
            }

            m.ElementsType = DetermineElementsType(m);
            PackVertices(m);
        }
        private static void CalculateTangents(Mesh m)
        {
            // Don't use any imported tangents
            m.Tangents = [];

            int numIndices = m.RawIndices.Length;
            var tangents = new Vector3[numIndices];
            var bitangents = new Vector3[numIndices];

            var positions = new Vector3[numIndices];
            for (uint i = 0; i < numIndices; i++)
            {
                positions[i] = m.Vertices[(int)m.Indices[(int)i]].Position;
            }

            for (uint i = 0; i < numIndices; i += 3)
            {
                uint i0 = i;
                uint i1 = i + 1;
                uint i2 = i + 2;

                var p0 = m.Positions[i0];
                var p1 = m.Positions[i1];
                var p2 = m.Positions[i2];

                var uv0 = m.Vertices[(int)m.Indices[(int)i0]].UV;
                var uv1 = m.Vertices[(int)m.Indices[(int)i1]].UV;
                var uv2 = m.Vertices[(int)m.Indices[(int)i2]].UV;

                var duv1 = new Vector2(uv1.X - uv0.X, uv1.Y - uv0.Y);
                var duv2 = new Vector2(uv2.X - uv0.X, uv2.Y - uv0.Y);

                var dp1 = p1 - p0;
                var dp2 = p2 - p0;

                float det = duv1.X * duv2.Y - duv1.Y * duv2.X;
                if (MathF.Abs(det) < float.Epsilon) det = float.Epsilon;
                float invDet = 1.0f / det;

                var t = (dp1 * duv2.Y - dp2 * duv1.Y) * invDet;
                var b = (dp2 * duv1.X - dp1 * duv2.X) * invDet;
                tangents[i0] += t;
                tangents[i1] += t;
                tangents[i2] += t;
                bitangents[i0] += b;
                bitangents[i1] += b;
                bitangents[i2] += b;
            }

            // Orthonormalize and calculate handedness
            for (uint i = 0; i < numIndices; i++)
            {
                var t = tangents[i];
                var b = bitangents[i];
                var n = m.Vertices[(int)m.Indices[(int)i]].Normal;

                var tangent = Vector3.Normalize(t - n * Vector3.Dot(n, t));
                float handedness = Vector3.Dot(Vector3.Cross(n, t), b);
                handedness = handedness > 0f ? 1f : -1f;

                m.Vertices[(int)m.Indices[(int)i]].Tangent = new Vector4(tangent, handedness);
            }
        }
        private static void ProcessTangents(Mesh m)
        {
            if (m.Tangents.Length != m.RawIndices.Length)
            {
                return;
            }

            uint numVertices = (uint)m.Vertices.Count;
            uint numIndices = (uint)m.Indices.Count;
            Debug.Assert(numVertices > 0 && numIndices > 0);

            Vertex[] oldVertices = [.. m.Vertices];
            m.Vertices.Clear();
            uint[] oldIndices = [.. m.Indices];
            m.Indices.Clear();
            m.Indices.AddRange(new uint[numIndices]);

            List<List<int>> idxRef = [];
            for (uint i = 0; i < numIndices; i++)
            {
                idxRef[(int)oldIndices[i]].Add((int)i);
            }

            for (uint i = 0; i < numVertices; i++)
            {
                var refs = idxRef[(int)i];
                int numRefs = refs.Count;

                for (uint j = 0; j < numRefs; j++)
                {
                    int jRef = refs[(int)j];

                    var tj = m.Tangents[jRef];
                    var v = oldVertices[(int)oldIndices[jRef]];
                    v.Tangent = tj;
                    m.Indices[jRef] = (uint)m.Vertices.Count;
                    m.Vertices.Add(v);

                    for (uint k = j + 1; k < numRefs; k++)
                    {
                        int kRef = refs[(int)k];

                        var t = m.Tangents[kRef];
                        if (Vector4NearEqual(tj, t, float.Epsilon))
                        {
                            m.Indices[kRef] = m.Indices[jRef];
                            refs.RemoveAt((int)k);
                            numRefs--;
                            k--;
                        }
                    }
                }
            }
        }
        private static bool Vector4NearEqual(Vector4 a, Vector4 b, float eps)
        {
            return
                MathF.Abs(a.X - b.X) <= eps &&
                MathF.Abs(a.Y - b.Y) <= eps &&
                MathF.Abs(a.Z - b.Z) <= eps &&
                MathF.Abs(a.W - b.W) <= eps;
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

            Dictionary<uint, List<uint>> idxRef = [];
            for (uint i = 0; i < numIndices; i++)
            {
                idxRef.TryAdd(m.RawIndices[i], []);
                idxRef[m.RawIndices[i]].Add(i);
            }

            for (uint i = 0; i < numVertices; i++)
            {
                var refs = idxRef[i];
                uint numRefs = (uint)refs.Count;

                for (uint j = 0; j < numRefs; j++)
                {
                    uint jRef = refs[(int)j];
                    Vertex v = new();
                    m.Indices[(int)jRef] = (uint)m.Vertices.Count;
                    v.Position = m.Positions[m.RawIndices[jRef]];
                    var n1 = m.Normals[m.RawIndices[jRef]];

                    if (!isHardEdge)
                    {
                        for (uint k = j + 1; k < numRefs; k++)
                        {
                            uint kRef = refs[(int)k];
                            float cosTheta = 0f; // this value represents the cosine of the angle between normals.
                            var n2 = m.Normals[m.RawIndices[kRef]];

                            if (!isSoftEdge)
                            {
                                cosTheta = Vector3.Dot(n1, n2) / n1.Length();
                            }

                            if (isSoftEdge || cosTheta >= cosAlpha)
                            {
                                n1 += n2;

                                m.Indices[(int)kRef] = m.Indices[(int)jRef];
                                refs.RemoveAt((int)k);
                                numRefs--;
                                k--;
                            }
                        }
                    }
                    v.Normal = Vector3.Normalize(n1);

                    m.Vertices.Add(v);
                }
            }
            Debug.Assert(m.Indices.Max() < m.Vertices.Count);
        }

        private static void ProcessUVs(Mesh m)
        {
            uint numVertices = (uint)m.Vertices.Count;
            uint numIndices = (uint)m.Indices.Count;
            Debug.Assert(numVertices > 0 && numIndices > 0);

            Vertex[] oldVertices = [.. m.Vertices];
            m.Vertices.Clear();
            uint[] oldIndices = [.. m.Indices];
            m.Indices.Clear();
            m.Indices.AddRange(new uint[numIndices]);

            Dictionary<uint, List<uint>> idxRef = [];
            for (uint i = 0; i < numIndices; i++)
            {
                idxRef.TryAdd(oldIndices[i], []);
                idxRef[oldIndices[i]].Add(i);
            }

            for (uint i = 0; i < numVertices; i++)
            {
                var refs = idxRef[i];
                uint numRefs = (uint)refs.Count;

                for (uint j = 0; j < numRefs; j++)
                {
                    uint jRef = refs[(int)j];
                    m.Indices[(int)jRef] = (uint)m.Vertices.Count;
                    var v = oldVertices[oldIndices[jRef]];
                    v.UV = m.UVSets[0][m.RawIndices[jRef]];
                    m.Vertices.Add(v);

                    for (uint k = j + 1; k < numRefs; k++)
                    {
                        uint kRef = refs[(int)k];
                        var uv = m.UVSets[0][m.RawIndices[kRef]];

                        if (Vector2NearEqual(v.UV, uv, float.Epsilon))
                        {
                            m.Indices[(int)kRef] = m.Indices[(int)jRef];
                            refs.RemoveAt((int)k);
                            numRefs--;
                            k--;
                        }
                    }
                }
            }
        }
        private static bool Vector2NearEqual(Vector2 a, Vector2 b, float eps)
        {
            return
                MathF.Abs(a.X - b.X) <= eps &&
                MathF.Abs(a.Y - b.Y) <= eps;
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

            int elementsCapacity = PackingHelper.GetVertexElementsSize(m.ElementsType) * numVertices;
            using MemoryStream msElementsType = new(elementsCapacity);

            for (int i = 0; i < numVertices; i++)
            {
                Processors()[m.ElementsType](msElementsType, m.Vertices[i]);
            }

            m.ElementBuffer = msElementsType.ToArray();
        }

        public static string PackData(Model model, string destinationFolder)
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

            string fileName = Path.Combine(destinationFolder, Path.ChangeExtension(model.Name, ".asset"));
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            blob.SaveToFile(fileName);

            return fileName;
        }
        public static string PackDataByLODGroup(LODGroup lodGroup, string destinationFolder)
        {
            int sceneSize = GetLODGroupSize(lodGroup);
            IntPtr buffer = Marshal.AllocHGlobal(sceneSize);

            BlobStreamWriter blob = new(buffer, sceneSize);

            // scene name
            blob.Write(lodGroup.Name);

            // number of LODs
            blob.Write(1);

            // LOD name
            blob.Write(lodGroup.Name);

            // number of meshes in this LOD
            blob.Write(lodGroup.Meshes.Count);

            foreach (var m in lodGroup.Meshes)
            {
                PackMesh(m, blob);
            }

            Debug.Assert(sceneSize == blob.Offset);

            string fileName = Path.Combine(destinationFolder, Path.ChangeExtension(lodGroup.Name, ".asset"));
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
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
        private static int GetLODGroupSize(LODGroup lodGroup)
        {
            int size;
            int sceneNameLength = lodGroup.Name.Length;

            size =
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            int lodSize;
            int lodNameLength = lodGroup.Name.Length;

            lodSize =
                sizeof(int) + lodNameLength +   // LOD name length and room for LOD name string
                sizeof(int);                    // number of meshes in this LOD

            foreach (var m in lodGroup.Meshes)
            {
                lodSize += GetMeshSize(m);
            }

            size += lodSize;

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
            Debug.Assert(elementBufferSize == PackingHelper.GetVertexElementsSize(m.ElementsType) * numVertices);
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
            int elementsSize = PackingHelper.GetVertexElementsSize(m.ElementsType);
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

            ElementsType elementsType = DetermineElementsType(firstMesh);
            int uvSetCount = firstMesh.UVSets.Length;
            uint lodId = firstMesh.LodId;
            float lodThreshold = firstMesh.LodThreshold;

            for (int meshIdx = 0; meshIdx < lod.Meshes.Count; meshIdx++)
            {
                var m = lod.Meshes[meshIdx];

                if (elementsType != DetermineElementsType(m) ||
                    uvSetCount != m.UVSets.Length ||
                    lodId != m.LodId ||
                    !NearEqual(lodThreshold, m.LodThreshold))
                {
                    combinedMesh = null;

                    return false;
                }
            }

            combinedMesh = new()
            {
                Name = firstMesh.Name,
                ElementsType = elementsType,
                LodId = lodId,
                LodThreshold = lodThreshold,
                UVSets = new Vector2[uvSetCount][],
            };

            for (int i = 0; i < uvSetCount; i++)
            {
                combinedMesh.UVSets[i] = [];
            }

            for (uint meshIdx = 0; meshIdx < lod.Meshes.Count; meshIdx++)
            {
                var m = lod.Meshes[(int)meshIdx];

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
