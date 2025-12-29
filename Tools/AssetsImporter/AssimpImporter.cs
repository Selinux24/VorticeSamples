using ContentTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Utilities;

namespace AssetsImporter
{
    using AAssimpContext = Assimp.AssimpContext;
    using AMesh = Assimp.Mesh;
    using ANode = Assimp.Node;
    using APostProcessSteps = Assimp.PostProcessSteps;
    using APropertyConfig = Assimp.Configs.PropertyConfig;
    using AScene = Assimp.Scene;

    public static class AssimpImporter
    {
        static readonly object mutex = new();
        static AScene aScene;
        static GeometryImportSettings importSettings;

        public static IEnumerable<string> Read(string fileName, GeometryImportSettings settings, string destinationFolder, Progression progression = null)
        {
            importSettings = settings;

            lock (mutex)
            {
                APostProcessSteps ppSteps =
                    APostProcessSteps.Triangulate |
                    APostProcessSteps.GlobalScale |
                    //APostProcessSteps.SortByPrimitiveType |
                    //APostProcessSteps.CalculateTangentSpace |
                    //APostProcessSteps.JoinIdenticalVertices |
                    //APostProcessSteps.OptimizeMeshes |
                    //APostProcessSteps.RemoveRedundantMaterials |
                    APostProcessSteps.ValidateDataStructure;

                APropertyConfig configs = new Assimp.Configs.FBXImportMaterialsConfig(true);

                aScene = ReadFile(fileName, ppSteps, configs);

                GetScene(progression, out var scene);

                Geometry.ProcessScene(scene, settings, progression);

                // Pack the scene data (for editor)
                foreach (var lodGroup in scene.LODGroups)
                {
                    yield return Assets.Create(lodGroup, destinationFolder);
                }
            }
        }
        static AScene ReadFile(string filePath, APostProcessSteps ppSteps = APostProcessSteps.None, params APropertyConfig[] configs)
        {
            Console.WriteLine($"Reading {filePath} with Assimp");

            using AAssimpContext importer = new();
            if (configs != null)
            {
                foreach (var config in configs)
                {
                    importer.SetConfig(config);
                }
            }

            return importer.ImportFile(filePath, ppSteps);
        }

        static bool GetScene(Progression progression, out Scene scene)
        {
            ANode root = aScene?.RootNode;
            if (root == null)
            {
                scene = default;
                return false;
            }

            scene = new(root.Name);

            int numNodes = root.ChildCount;

            if (importSettings.CoalesceMeshes)
            {
                LODGroup lod = new();
                for (int i = 0; i < numNodes; i++)
                {
                    var node = root.Children[i];
                    if (node == null)
                    {
                        continue;
                    }

                    lod.Meshes.AddRange(GetMeshes(scene, node, 0, -1f, progression));
                }

                if (lod.Meshes.Count > 0)
                {
                    lod.Name = lod.Meshes[0].Name;

                    if (Geometry.CoalesceMeshes(lod, progression, out var combinedMesh))
                    {
                        lod.Meshes.Clear();
                        lod.Meshes.Add(combinedMesh);
                    }
                    scene.LODGroups.Add(lod);
                }
            }
            else
            {
                for (int i = 0; i < numNodes; i++)
                {
                    var node = root.Children[i];
                    if (node == null)
                    {
                        continue;
                    }

                    LODGroup lod = new();
                    lod.Meshes.AddRange(GetMeshes(scene, node, 0, -1f, progression));
                    if (lod.Meshes.Count > 0)
                    {
                        lod.Name = lod.Meshes[0].Name;
                        scene.LODGroups.Add(lod);
                    }
                }
            }

            return true;
        }
        static Mesh[] GetMeshes(Scene scene, ANode node, uint lodId, float lodThreshold, Progression progression)
        {
            Debug.Assert(node != null && lodId != uint.MaxValue);
            bool isLodGroup = false;

            List<Mesh> meshes = [];

            if (node.HasMeshes)
            {
                for (int m = 0; m < node.MeshCount; m++)
                {
                    var aMesh = aScene.Meshes[node.MeshIndices[m]];
                    meshes.AddRange(GetMesh(aMesh, lodId, lodThreshold, progression));
                }
            }
            else if (importSettings.IsLOD)
            {
                GetLODGroup(scene, node, progression);
                isLodGroup = true;
            }

            if (!isLodGroup)
            {
                for (int i = 0; i < node.ChildCount; i++)
                {
                    meshes.AddRange(GetMeshes(scene, node.Children[i], lodId, lodThreshold, progression));
                }
            }

            return [.. meshes];
        }
        static Mesh[] GetMesh(AMesh aMesh, uint lodId, float lodThreshold, Progression progression)
        {
            List<Mesh> meshes = [];

            Debug.Assert(aMesh != null);

            if (GetMeshData(aMesh, out var m))
            {
                m.LodId = lodId;
                m.LodThreshold = lodThreshold;
                m.Name = aMesh.Name;

                meshes.Add(m);
                progression?.Callback(progression.Value, progression.MaxValue + 1);
            }

            return [.. meshes];
        }
        static void GetLODGroup(Scene scene, ANode node, Progression progression)
        {
            Debug.Assert(node != null);

            LODGroup lod = new()
            {
                Name = node.Name
            };

            // NOTE: number of LODs is exclusive the base mesh (LOD 0)
            int numNodes = node.ChildCount;
            Debug.Assert(numNodes > 0 && importSettings.Thresholds.Length == (numNodes - 1));

            for (int i = 0; i < numNodes; i++)
            {
                float lodThreshold = -1f;
                if (i > 0)
                {
                    lodThreshold = importSettings.Thresholds[i - 1];
                }

                lod.Meshes.AddRange(GetMeshes(scene, node.Children[i], (uint)lod.Meshes.Count, lodThreshold, progression));
            }

            if (lod.Meshes.Count > 0)
            {
                scene.LODGroups.Add(lod);
            }
        }

        static (Vector3[], uint[]) GetControlPoints(AMesh aMesh)
        {
            List<Vector3> controlPoints = [];
            for (int i = 0; i < aMesh.VertexCount; i++)
            {
                if (controlPoints.Any(p => Utils.Vector3NearEqual(aMesh.Vertices[i], p, float.Epsilon))) continue;

                controlPoints.Add(aMesh.Vertices[i]);
            }

            // Remap indices to control points
            uint[] indices = [.. aMesh.GetUnsignedIndices()];
            if (indices.Length == 0)
            {
                // Populate indices
                indices = new uint[aMesh.Vertices.Count];
                for (uint i = 0; i < aMesh.Vertices.Count; i++)
                {
                    indices[i] = i;
                }
            }

            uint[] remappedIndices = new uint[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                // For each vertex, find the control point indices
                var v = aMesh.Vertices[(int)indices[i]];
                remappedIndices[i] = (uint)controlPoints.FindIndex(c => Utils.Vector3NearEqual(c, v, float.Epsilon));
            }

            Debug.Assert(controlPoints.Count > 0 && remappedIndices.Length > 0);
            Debug.Assert(remappedIndices.Max() == controlPoints.Count - 1);
            Debug.Assert(remappedIndices.Length % 3 == 0);

            return ([.. controlPoints], remappedIndices);
        }
        static Vector3[] ExpandNormals(AMesh aMesh)
        {
            List<Vector3> res = [];

            int[] indices = [.. aMesh.GetIndices()];
            for (uint i = 0; i < indices.Length; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                var n0 = aMesh.Normals[i0];
                var n1 = aMesh.Normals[i1];
                var n2 = aMesh.Normals[i2];

                res.Add(n0);
                res.Add(n1);
                res.Add(n2);
            }

            return [.. res];
        }
        static Vector2[] ExpandUVSets(AMesh aMesh, int channel)
        {
            List<Vector2> res = [];

            Vector2[] uvs = [.. aMesh.TextureCoordinateChannels[channel].Select(uv => new Vector2(uv.X, 1f - uv.Y))];

            int[] indices = [.. aMesh.GetIndices()];
            for (uint i = 0; i < indices.Length; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                var t0 = uvs[i0];
                var t1 = uvs[i1];
                var t2 = uvs[i2];

                res.Add(t0);
                res.Add(t1);
                res.Add(t2);
            }

            return [.. res];
        }
        static Vector4[] ExpandTangents(AMesh aMesh)
        {
            List<Vector4> res = [];

            var tangents = aMesh.Tangents
                .Select((t, i) =>
                {
                    var tn = Vector3.Normalize(t);
                    var nn = Vector3.Normalize(aMesh.Normals[i]);
                    var bn = Vector3.Normalize(aMesh.BiTangents[i]);

                    // Calculate handedness per vertex: w = sign( dot( cross(normal, tangent), bitangent ) )
                    float handedness = Vector3.Dot(Vector3.Cross(nn, tn), bn) < 0f ? 1f : -1f;
                    return new Vector4(tn, handedness);
                })
                .ToArray();

            int[] indices = [.. aMesh.GetIndices()];
            for (uint i = 0; i < indices.Length; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                var n0 = tangents[i0];
                var n1 = tangents[i1];
                var n2 = tangents[i2];

                res.Add(n0);
                res.Add(n1);
                res.Add(n2);
            }

            return [.. res];
        }

        static bool GetMeshData(AMesh aMesh, out Mesh m)
        {
            Debug.Assert(aMesh != null);

            // Get vertices
            if (!aMesh.HasVertices)
            {
                m = default;
                return false;
            }

            List<Vector3> positions = [];
            List<Vector3> normals = [];
            List<Vector4> tangents = [];
            List<Vector2[]> uvSets = [];
            List<int> materialIndices = [];

            var (vertices, indices) = GetControlPoints(aMesh);

            uint[] rawIndices = new uint[indices.Length];
            Array.Fill(rawIndices, uint.MaxValue);

            uint[] vertexRef = new uint[vertices.Length];
            Array.Fill(vertexRef, uint.MaxValue);

            for (uint i = 0; i < indices.LongLength; i++)
            {
                uint vIdx = indices[i];

                // Did we encounter this vertex before? If so, just add its index.
                // If not, add the vertex and a new index.
                if (vertexRef[vIdx] != uint.MaxValue)
                {
                    rawIndices[i] = vertexRef[vIdx];
                }
                else
                {
                    var v = vertices[vIdx];
                    rawIndices[i] = (uint)positions.Count;
                    vertexRef[vIdx] = rawIndices[i];
                    positions.Add(v);
                }
            }
            Debug.Assert(rawIndices.Length % 3 == 0);

            materialIndices.Add(aMesh.MaterialIndex);

            if (!importSettings.CalculateNormals)
            {
                if (aMesh.HasNormals)
                {
                    normals.AddRange(ExpandNormals(aMesh));
                }
                else
                {
                    importSettings.CalculateNormals = true;
                }
            }

            if (!importSettings.CalculateTangents)
            {
                if (aMesh.HasTangentBasis)
                {
                    tangents.AddRange(ExpandTangents(aMesh));
                }
                else
                {
                    importSettings.CalculateTangents = true;
                }
            }

            for (int i = 0; i < aMesh.TextureCoordinateChannelCount; i++)
            {
                // Assuming FBX UVs always have their origin at the bottom-left, the V-axis 
                // should be flipped, since DirectX uses the upper-left corner as the origin.
                // TODO: May be assimp does this yet?
                uvSets.Add(ExpandUVSets(aMesh, i));
            }

            m = new()
            {
                Name = aMesh.Name,

                Positions = [.. positions],
                RawIndices = [.. rawIndices],

                Normals = [.. normals],
                Tangents = [.. tangents],
                UVSets = [.. uvSets],
                MaterialIndices = [.. materialIndices],

                LodThreshold = -1f,
            };

            return true;
        }

        public static void PackForEngine(string assetFilename, string contentFilename)
        {
            string output = Path.GetFullPath(contentFilename);
            if (File.Exists(output))
            {
                File.Delete(output);
            }
            string outputDir = Path.GetDirectoryName(output);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Read the packed data (to editor)
            var geometry = GeometryToEngine.ReadData(assetFilename);
            Debug.Assert(geometry?.LODGroups?.Count > 0);

            // Pack the editor data (for engine)
            GeometryToEngine.PackForEngine(geometry, output);
        }
    }
}
