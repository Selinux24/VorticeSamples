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

    class AssimpImporter
    {
        private readonly Scene scene = null;
        private AScene aScene = null;

        public AssimpImporter(string filePath, Scene scene)
        {
            this.scene = scene;

            APostProcessSteps steps =
                APostProcessSteps.ValidateDataStructure |
                APostProcessSteps.GlobalScale |
                APostProcessSteps.Triangulate |
                APostProcessSteps.GenerateNormals |
                APostProcessSteps.CalculateTangentSpace;

            var scaleConfig = new AFBXConvertToMetersConfig(true);

            Import(filePath, steps, [scaleConfig]);
        }

        private void Import(
            string filePath,
            APostProcessSteps ppSteps = APostProcessSteps.None,
            APropertyConfig[] configs = null)
        {
            Console.WriteLine($"Importing {filePath} with Assimp");

            using AAssimpContext importer = new();
            if (configs != null)
            {
                foreach (var config in configs)
                {
                    importer.SetConfig(config);
                }
            }

            aScene = importer.ImportFile(filePath, ppSteps);
        }

        private void GetScene()
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
                Vector3[] vertices = [.. aMesh.Vertices];
                uint[] indices = [.. aMesh.GetUnsignedIndices()];
                Debug.Assert(vertices.Length > 0 && indices.Length > 0);
                if (vertices.Length <= 0 || indices.Length <= 0)
                {
                    continue;
                }
                Debug.Assert(indices.Length % 3 == 0);
                mesh.Positions.AddRange(vertices);
                mesh.RawIndices.AddRange(indices);

                Vector3[] normals = [.. aMesh.Normals];
                Vector3[] tangents = [.. aMesh.Tangents];
                mesh.Normals.AddRange(normals);
                mesh.Tangents.AddRange(tangents);

                mesh.MaterialIndices.Add(aMesh.MaterialIndex);
                if (!mesh.MaterialUsed.Contains(aMesh.MaterialIndex))
                {
                    mesh.MaterialUsed.Add(aMesh.MaterialIndex);
                }

                for (int i = 0; i < aMesh.TextureCoordinateChannelCount; i++)
                {
                    Vector2[] uvs = [.. aMesh.TextureCoordinateChannels[i].Select(uv => new Vector2(uv.X, uv.Y))];
                    mesh.UVSets.Add(new(uvs));
                }
            }
        }

        private static readonly Mutex mutex = new();

        public static void Import(string filePath, SceneData data)
        {
            Scene scene = new("Import Scene");

            lock (mutex)
            {
                AssimpImporter importer = new(filePath, scene);

                importer.GetScene();
            }

            Geometry.ProcessScene(scene, data.Settings);
            Geometry.PackData(scene, data);
        }
    }
}
