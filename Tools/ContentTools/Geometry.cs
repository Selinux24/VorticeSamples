using ContentTools.MikkTSpace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Utilities;

namespace ContentTools
{
    /// <summary>
    /// Geometry utils
    /// </summary>
    public static class Geometry
    {
        #region Classes

        class MeshMikkTSpaceGenerator(Mesh m) : MikkTSpaceGenerator<Mesh>(m)
        {
            public static void CalculateMikkTSpace(Mesh m)
            {
                // Don't use any imported tangents
                m.Tangents = [];

                MeshMikkTSpaceGenerator mikkTSpace = new(m);
                mikkTSpace.GenTangSpace();
            }

            public override int GetNumFaces()
            {
                var m = GetMesh();
                return m.Indices.Count / 3;
            }
            public override int GetNumVerticesOfFace(int faceIndex)
            {
                return 3;
            }
            public override Vector3 GetPosition(int faceIndex, int vertIndex)
            {
                var m = GetMesh();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].Position;
            }
            public override Vector3 GetNormal(int faceIndex, int vertIndex)
            {
                var m = GetMesh();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].Normal;
            }
            public override Vector2 GetTexCoord(int faceIndex, int vertIndex)
            {
                var m = GetMesh();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                return m.Vertices[(int)index].UV;
            }
            public override void SetTSpaceBasic(Vector3 tangent, float sign, int faceIndex, int vertIndex)
            {
                var m = GetMesh();
                uint index = m.Indices[faceIndex * 3 + vertIndex];
                var v = m.Vertices[(int)index];
                v.Tangent = new Vector4(tangent, sign);
                m.Vertices[(int)index] = v;
            }
        }

        #endregion

        static readonly Dictionary<ElementsType, VertexProcessor> processors = [];

        public static Dictionary<ElementsType, VertexProcessor> Processors()
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

        public static void ProcessScene(Scene scene, GeometryImportSettings settings, Progression progression = null)
        {
            SplitMeshesByMaterial(scene, progression);

            foreach (var lod in scene.LODGroups)
            {
                foreach (var m in lod.Meshes)
                {
                    ProcessVertices(m, settings);

                    progression?.Callback(progression.Value + 1, progression.MaxValue);
                }
            }
        }
        static void SplitMeshesByMaterial(Scene scene, Progression progression = null)
        {
            progression?.Callback(0, 0);

            foreach (var lod in scene.LODGroups)
            {
                //Find meshes that use more than one material and split them.
                if (lod.Meshes.All(m => m.MaterialUsed.Length <= 1))
                {
                    progression?.Callback(progression.Value, progression.MaxValue + (uint)lod.Meshes.Count);
                    continue;
                }

                List<Mesh> newMeshes = [];

                foreach (var m in lod.Meshes)
                {
                    // If more than one material is used in this mesh
                    // then split it into submeshes.
                    int numMaterials = m.MaterialUsed.Length;
                    if (numMaterials > 1)
                    {
                        for (int i = 0; i < numMaterials; i++)
                        {
                            if (SplitMeshesByMaterial(m.MaterialUsed[i], m, out var submesh))
                            {
                                newMeshes.Add(submesh);
                            }
                        }
                    }
                    else
                    {
                        newMeshes.Add(m);
                    }
                }

                progression?.Callback(progression.Value, progression.MaxValue + (uint)newMeshes.Count);

                lod.Meshes.Clear();
                lod.Meshes.AddRange(newMeshes);
            }
        }
        static bool SplitMeshesByMaterial(int materialIdx, Mesh m, out Mesh submesh)
        {
            List<uint> rawIndices = [];
            List<Vector3> positions = [];
            List<Vector3> normals = [];
            List<Vector4> tangents = [];
            List<Vector2>[] uvSets = new List<Vector2>[m.UVSets.Length];
            Array.Fill(uvSets, []);

            int numPolys = m.RawIndices.Length / 3;
            uint[] vertexRef = new uint[m.Positions.Length];
            Array.Fill(vertexRef, uint.MaxValue);

            for (uint i = 0; i < numPolys; i++)
            {
                int mtlIdx = m.MaterialIndices[i];
                if (mtlIdx != materialIdx) continue;

                uint index = i * 3;
                for (uint j = index; j < index + 3; j++)
                {
                    uint vIdx = m.RawIndices[j];
                    if (vertexRef[vIdx] != uint.MaxValue)
                    {
                        rawIndices.Add(vertexRef[vIdx]);
                    }
                    else
                    {
                        rawIndices.Add((uint)positions.Count);
                        vertexRef[vIdx] = (uint)rawIndices.Count;
                        positions.Add(m.Positions[vIdx]);
                    }

                    if (m.Normals.Length > 0)
                    {
                        normals.Add(m.Normals[j]);
                    }

                    if (m.Tangents.Length > 0)
                    {
                        tangents.Add(m.Tangents[j]);
                    }

                    for (int k = 0; k < m.UVSets.Length; k++)
                    {
                        if (m.UVSets[k].Length > 0)
                        {
                            uvSets[k].Add(m.UVSets[k][j]);
                        }
                    }
                }
            }

            Debug.Assert((rawIndices.Count % 3) == 0);

            submesh = new()
            {
                Name = m.Name,
                LodThreshold = m.LodThreshold,
                LodId = m.LodId,
                RawIndices = [.. rawIndices],
                Positions = [.. positions],
                Normals = [.. normals],
                Tangents = [.. tangents],
                UVSets = [.. uvSets.Select(s => s.ToArray())]
            };

            return submesh.RawIndices.Length != 0;
        }

        static ElementsType DetermineElementsType(Mesh m)
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

        static void ProcessVertices(Mesh m, GeometryImportSettings settings)
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
                MeshMikkTSpaceGenerator.CalculateMikkTSpace(m);
            }

            // NOTE: m.tangents contains values of the imported tangent vectors. It will be empty
            //       if tangents where calculated. Therefore, process_tangents is only called
            //       when tangents are imported from the source file.
            if (m.Tangents.Length > 0)
            {
                ProcessTangents(m);
            }

            m.ElementsType = DetermineElementsType(m);

            m.PackVertices();
        }
        static void RecalculateNormals(Mesh m)
        {
            uint numIndices = (uint)m.RawIndices.LongLength;
            m.Normals = new Vector3[numIndices];

            for (uint i = 0; i < numIndices; i += 3)
            {
                uint i0 = m.RawIndices[i + 0];
                uint i1 = m.RawIndices[i + 1];
                uint i2 = m.RawIndices[i + 2];

                var v0 = m.Positions[i0];
                var v1 = m.Positions[i1];
                var v2 = m.Positions[i2];

                var e0 = v1 - v0;
                var e1 = v2 - v0;
                var n = Vector3.Cross(e0, e1);

                m.Normals[i + 0] = n;
                m.Normals[i + 1] = n;
                m.Normals[i + 2] = n;
            }
        }
        static void ProcessNormals(Mesh m, float smoothingAngle)
        {
            int numIndices = m.RawIndices.Length;
            int numVertices = m.Positions.Length;
            Debug.Assert(numIndices > 0 && numVertices > 0);

            float cosAlpha = MathF.Cos(MathF.PI - smoothingAngle * MathF.PI / 180f);
            bool isHardEdge = MathF.Abs(smoothingAngle - 180f) < Utils.Epsilon;
            bool isSoftEdge = MathF.Abs(smoothingAngle) < Utils.Epsilon;

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
                    var n1 = m.Normals[jRef];

                    if (!isHardEdge)
                    {
                        for (uint k = j + 1; k < numRefs; k++)
                        {
                            uint kRef = refs[(int)k];
                            float cosTheta = 0f; // this value represents the cosine of the angle between normals.
                            var n2 = m.Normals[kRef];

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
        static void ProcessUVs(Mesh m)
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
                    v.UV = m.UVSets[0][jRef];
                    m.Vertices.Add(v);

                    for (uint k = j + 1; k < numRefs; k++)
                    {
                        uint kRef = refs[(int)k];
                        var uv = m.UVSets[0][kRef];

                        if (Utils.NearEqual(v.UV, uv))
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
        static void ProcessTangents(Mesh m)
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
                        if (Utils.NearEqual(tj, t))
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
                    !Utils.NearEqual(lodThreshold, m.LodThreshold))
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
        static void AppendToVectorPod<T>(ref T[] dst, T[] src)
        {
            if (src.Length == 0)
            {
                return;
            }

            int numElements = dst.Length;
            Array.Resize(ref dst, dst.Length + src.Length);
            Array.Copy(src, 0, dst, numElements, src.Length);
        }
    }
}
