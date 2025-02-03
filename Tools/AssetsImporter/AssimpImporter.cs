using ContentTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace AssetsImporter
{
    using AAssimpContext = Assimp.AssimpContext;
    using AFBXConvertToMetersConfig = Assimp.Configs.FBXConvertToMetersConfig;
    using APostProcessSteps = Assimp.PostProcessSteps;
    using APropertyConfig = Assimp.Configs.PropertyConfig;
    using AScene = Assimp.Scene;

    public static class AssimpImporter
    {
        private static readonly object mutex = new();

        public static string[] Read(string fileName, GeometryImportSettings settings, string assetsFolder)
        {
            lock (mutex)
            {
                List<string> files = [];

                var models = ReadScene(fileName, settings);
                foreach (var model in models)
                {
                    // Process the scene data
                    Geometry.ProcessScene(model, settings);

                    // Pack the scene data (for editor)
                    files.Add(Geometry.PackData(model, assetsFolder));
                }

                return [.. files];
            }
        }
        public static void Import(string assetFilename, string contentFilename)
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
            var d = GeometryToEngine.ReadData(assetFilename);

            // Pack the editor data (for engine)
            GeometryToEngine.PackForEngine(d, output);

            Console.WriteLine("Asset imported successfully.");
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
        private static Geometry.Model[] ReadScene(string fileName, GeometryImportSettings settings)
        {
            var aScene = ReadFile(
                fileName,
                APostProcessSteps.Triangulate,
                [new AFBXConvertToMetersConfig(true)]);

            float unitScaleFactor = 1f;
            if (aScene.Metadata.TryGetValue("UnitScaleFactor", out var scaleFactor))
            {
                unitScaleFactor = (float)scaleFactor.Data;
            }

            List<Geometry.Model> models = [];

            foreach (var aMesh in aScene.Meshes)
            {
                // Get vertices
                if (!aMesh.HasVertices)
                {
                    continue;
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

                materialIndices.Add(aMesh.MaterialIndex);

                if (settings.CalculateNormals)
                {
                    if (aMesh.HasNormals)
                    {
                        normals.AddRange(aMesh.Normals);
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
                        tangents.AddRange(aMesh.Tangents.Select(t => new Vector4(t, 0f)));
                    }
                    else
                    {
                        settings.CalculateTangents = true;
                    }
                }

                for (uint i = 0; i < aMesh.TextureCoordinateChannelCount; i++)
                {
                    Vector2[] uvs = [.. aMesh.TextureCoordinateChannels[i].Select(uv => new Vector2(uv.X, uv.Y))];
                    uvSets.Add(uvs);
                }

                Geometry.Mesh mesh = new()
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
    }
}
