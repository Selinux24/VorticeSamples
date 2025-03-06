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
        private static readonly object mutex = new();
        private static AScene aScene;
        private static GeometryImportSettings importSettings;

        public static string[] Read(string fileName, GeometryImportSettings settings, string assetsFolder, Progression progression = null)
        {
            importSettings = settings;

            lock (mutex)
            {
                aScene = ReadFile(
                    fileName,
                    APostProcessSteps.Triangulate |
                    APostProcessSteps.SortByPrimitiveType |
                    APostProcessSteps.CalculateTangentSpace |
                    APostProcessSteps.JoinIdenticalVertices |
                    APostProcessSteps.OptimizeMeshes |
                    APostProcessSteps.RemoveRedundantMaterials |
                    APostProcessSteps.ValidateDataStructure |
                    APostProcessSteps.GlobalScale);

                GetModel(progression, out var model);

                // Pack the scene data (for editor)
                return [Geometry.PackData(model, assetsFolder)];

                /*
                List<string> files = [];

                var models = ReadScene();

                foreach (var model in models)
                {
                    // Process the scene data
                    Geometry.ProcessModel(model, settings, progression);

                    if (settings.CoalesceMeshes)
                    {
                        foreach (var lod in model.LODGroups)
                        {
                            if (lod.Meshes.Count <= 1)
                            {
                                continue;
                            }

                            if (Geometry.CoalesceMeshes(lod, progression, out var combinedMesh))
                            {
                                lod.Meshes.Clear();
                                lod.Meshes.Add(combinedMesh);
                            }
                        }
                    }

                    // Pack the scene data (for editor)
                    files.Add(Geometry.PackData(model, assetsFolder));
                }

                return [.. files];
                */
            }
        }
        private static AScene ReadFile(string filePath, APostProcessSteps ppSteps = APostProcessSteps.None, APropertyConfig[] configs = null)
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
        private static Geometry.Model[] ReadScene()
        {
            List<Geometry.Model> models = [];

            foreach (var aMesh in aScene.Meshes)
            {
                if (!GetMeshData(aMesh, out var mesh))
                {
                    continue;
                }

                Geometry.LODGroup lod = new()
                {
                    Name = aMesh.Name,
                    Meshes = [mesh],
                };

                models.Add(new Geometry.Model(aMesh.Name)
                {
                    LODGroups = [lod],
                });
            }

            return [.. models];
        }

        private static void GetModel(Progression progression, out Geometry.Model model)
        {
            ANode root = aScene?.RootNode;
            if (root == null)
            {
                model = default;
                return;
            }

            model = new(root.Name);

            int numNodes = root.ChildCount;

            if (importSettings.CoalesceMeshes)
            {
                Geometry.LODGroup lod = new();
                for (int i = 0; i < numNodes; i++)
                {
                    var node = root.Children[i];
                    if (node == null)
                    {
                        continue;
                    }

                    GetMeshes(model, node, lod.Meshes, 0, -1f, progression);
                }

                if (lod.Meshes.Count > 0)
                {
                    lod.Name = lod.Meshes[0].Name;

                    if (Geometry.CoalesceMeshes(lod, progression, out var combinedMesh))
                    {
                        lod.Meshes.Clear();
                        lod.Meshes.Add(combinedMesh);
                    }
                    model.LODGroups.Add(lod);
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

                    Geometry.LODGroup lod = new();
                    GetMeshes(model, node, lod.Meshes, 0, -1f, progression);
                    if (lod.Meshes.Count > 0)
                    {
                        lod.Name = lod.Meshes[0].Name;
                        model.LODGroups.Add(lod);
                    }
                }
            }
        }
        private static void GetMeshes(Geometry.Model model, ANode node, List<Geometry.Mesh> meshes, uint lodId, float lodThreshold, Progression progression)
        {
            Debug.Assert(node != null && lodId != uint.MaxValue);
            bool isLodGroup = false;

            if (node.HasMeshes)
            {
                for (int m = 0; m < node.MeshCount; m++)
                {
                    var mesh = aScene.Meshes[node.MeshIndices[m]];
                    GetMesh(mesh, meshes, lodId, lodThreshold, progression);
                }
            }
            else if (importSettings.IsLOD)
            {
                GetLODGroup(model, node, progression);
                isLodGroup = true;
            }

            if (!isLodGroup)
            {
                for (int i = 0; i < node.ChildCount; i++)
                {
                    GetMeshes(model, node.Children[i], meshes, lodId, lodThreshold, progression);
                }
            }
        }
        private static void GetMesh(AMesh mesh, List<Geometry.Mesh> meshes, uint lodId, float lodThreshold, Progression progression)
        {
            Debug.Assert(mesh != null);

            if (GetMeshData(mesh, out var m))
            {
                m.LodId = lodId;
                m.LodThreshold = lodThreshold;
                m.Name = mesh.Name;

                meshes.Add(m);
                progression?.Callback(progression.Value, progression.MaxValue + 1);
            }
        }
        private static void GetLODGroup(Geometry.Model model, ANode node, Progression progression)
        {
            Debug.Assert(node != null);

            Geometry.LODGroup lod = new()
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

                GetMeshes(model, node.Children[i], lod.Meshes, (uint)lod.Meshes.Count, lodThreshold, progression);
            }

            if (lod.Meshes.Count > 0)
            {
                model.LODGroups.Add(lod);
            }
        }
        private static bool GetMeshData(AMesh aMesh, out Geometry.Mesh m)
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

            Vector3[] vertices = [.. aMesh.Vertices];
            uint[] indices = [.. aMesh.GetUnsignedIndices()];
            if (indices.Length == 0)
            {
                Debug.Assert(vertices.Length % 3 == 0);

                // Populate indices
                indices = new uint[vertices.Length];
                for (uint i = 0; i < vertices.Length; i++)
                {
                    indices[i] = i;
                }
            }
            Debug.Assert(indices.Max() == vertices.Length - 1);

            uint[] rawIndices = new uint[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                rawIndices[i] = uint.MaxValue;
            }
            uint[] vertexRef = new uint[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexRef[i] = uint.MaxValue;
            }

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
            Debug.Assert(vertices.Length > 0 && indices.Length > 0);
            Debug.Assert(indices.Length % 3 == 0);
            Debug.Assert(indices.Max() == positions.Count - 1);

            materialIndices.Add(aMesh.MaterialIndex);

            bool importNormals = !importSettings.CalculateNormals;

            if (importNormals)
            {
                if (aMesh.HasNormals)
                {
                    normals.AddRange(aMesh.Normals);
                }
                else
                {
                    importSettings.CalculateNormals = true;
                }
            }

            bool importTangents = !importSettings.CalculateTangents;

            if (importTangents)
            {
                if (aMesh.HasTangentBasis)
                {
                    tangents.AddRange(aMesh.Tangents.Select(t => new Vector4(t, 0f)));
                }
                else
                {
                    importSettings.CalculateTangents = true;
                }
            }

            for (uint i = 0; i < aMesh.TextureCoordinateChannelCount; i++)
            {
                Vector2[] uvs = [.. aMesh.TextureCoordinateChannels[i].Select(uv => new Vector2(uv.X, uv.Y))];
                uvSets.Add(uvs);
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
