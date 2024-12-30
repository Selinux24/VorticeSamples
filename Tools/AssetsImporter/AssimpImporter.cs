using ContentTools;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace AssetsImporter
{
    using AAssimpContext = Assimp.AssimpContext;
    using AFBXConvertToMetersConfig = Assimp.Configs.FBXConvertToMetersConfig;
    using APostProcessSteps = Assimp.PostProcessSteps;
    using APropertyConfig = Assimp.Configs.PropertyConfig;
    using AScene = Assimp.Scene;

    static class AssimpImporter
    {
        private static readonly Mutex mutex = new();
        private static Scene scene = null;

        public static void Add(string filePath, SceneData sceneData)
        {
            scene ??= new(sceneData.Name);

            lock (mutex)
            {
                APostProcessSteps steps =
                    APostProcessSteps.ValidateDataStructure |
                    APostProcessSteps.GlobalScale |
                    APostProcessSteps.Triangulate |
                    APostProcessSteps.GenerateNormals |
                    APostProcessSteps.CalculateTangentSpace;
                var scaleConfig = new AFBXConvertToMetersConfig(true);

                var aScene = ReadFile(filePath, steps, [scaleConfig]);
                ReadScene(aScene, sceneData.Settings);
            }
        }
        public static void Import(SceneData sceneData)
        {
            Geometry.ProcessScene(scene, sceneData.Settings);
            Geometry.PackData(scene, sceneData);
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
        private static void ReadScene(AScene aScene, GeometryImportSettings settings)
        {
            foreach (var aMesh in aScene.Meshes)
            {
                Mesh mesh = new()
                {
                    LODId = 0,
                    LODThreshold = -1f,
                    Name = aMesh.Name
                };

                LODGroup lod = new()
                {
                    Name = aMesh.Name
                };
                lod.Meshes.Add(mesh);

                scene.LODGroups.Add(lod);

                // Get vertices
                if (!aMesh.HasVertices)
                {
                    continue;
                }
                Vector3[] vertices = [.. aMesh.Vertices];
                uint[] indices = [.. aMesh.GetUnsignedIndices()];
                uint[] vertexRef = new uint[vertices.Length];

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

                for (int i = 0; i < indices.Length; i++)
                {
                    mesh.RawIndices.Add(uint.MaxValue);
                }
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertexRef[i] = uint.MaxValue;
                }

                for (int i = 0; i < indices.Length; i++)
                {
                    uint vIdx = indices[i];

                    // Did we encounter this vertex before? If so, just add its index.
                    // If not, add the vertex and a new index.
                    if (vertexRef[vIdx] != uint.MaxValue)
                    {
                        mesh.RawIndices[i] = vertexRef[vIdx];
                    }
                    else
                    {
                        var v = vertices[vIdx];
                        mesh.RawIndices[i] = (uint)mesh.Positions.Count;
                        vertexRef[vIdx] = mesh.RawIndices[i];
                        mesh.Positions.Add(v);
                    }
                }
                Debug.Assert(vertices.Length > 0 && indices.Length > 0);
                Debug.Assert(indices.Length % 3 == 0);

                mesh.MaterialIndices.Add(aMesh.MaterialIndex);

                if (settings.CalculateNormals)
                {
                    if (aMesh.HasNormals)
                    {
                        mesh.Normals.AddRange(aMesh.Normals);
                    }
                    else
                    {
                        settings.CalculateNormals = true;
                    }
                }

                if (settings.CalculateTangents)
                {
                    if (aMesh.HasTangentBasis)
                    {
                        mesh.Tangents.AddRange(aMesh.Tangents.Select(t => new Vector4(t, 0f)));
                    }
                    else
                    {
                        settings.CalculateTangents = true;
                    }
                }

                for (int i = 0; i < aMesh.TextureCoordinateChannelCount; i++)
                {
                    Vector2[] uvs = [.. aMesh.TextureCoordinateChannels[i].Select(uv => new Vector2(uv.X, uv.Y))];
                    mesh.UVSets.Add(new(uvs));
                }
            }
        }
    }
}
