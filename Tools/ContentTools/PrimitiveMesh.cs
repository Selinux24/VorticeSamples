using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace ContentTools
{
    public static class PrimitiveMesh
    {
        private static readonly PrimitiveMeshCreator[] Creators =
        [
            CreatePlane,
            CreateCube,
            CreateUvSphere,
            CreateIcoSphere,
            CreateCylinder,
            CreateCapsule
        ];

        private static void CreatePlane(Scene scene, PrimitiveInitInfo info)
        {
            var lod = new LODGroup { Name = "plane" };
            lod.Meshes.Add(CreatePlane(info));
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreatePlane(PrimitiveInitInfo info, int horizontalIndex = 0, int verticalIndex = 2, bool flipWinding = false, Vector3 offset = default, Vector2 uRange = default, Vector2 vRange = default)
        {
            Debug.Assert(horizontalIndex < 3 && verticalIndex < 3);
            Debug.Assert(horizontalIndex != verticalIndex);

            if (offset == default) offset = new Vector3(-0.5f, 0f, -0.5f);
            if (uRange == default) uRange = new Vector2(0f, 1f);
            if (vRange == default) vRange = new Vector2(0f, 1f);

            int horizontalCount = (int)Math.Clamp(info.Segments[horizontalIndex], 1, 10);
            int verticalCount = (int)Math.Clamp(info.Segments[verticalIndex], 1, 10);
            float horizontalStep = 1f / horizontalCount;
            float verticalStep = 1f / verticalCount;
            float uStep = (uRange.Y - uRange.X) / horizontalCount;
            float vStep = (vRange.Y - vRange.X) / verticalCount;

            var mesh = new Mesh();
            var uvs = new List<Vector2>();

            for (var j = 0; j <= verticalCount; ++j)
            {
                for (var i = 0; i <= horizontalCount; ++i)
                {
                    var position = offset;
                    var asArray = new[] { position.X, position.Y, position.Z };
                    asArray[horizontalIndex] += i * horizontalStep;
                    asArray[verticalIndex] += j * verticalStep;
                    mesh.Positions.Add(new Vector3(asArray[0] * info.Size.X, asArray[1] * info.Size.Y, asArray[2] * info.Size.Z));

                    var uv = new Vector2(uRange.X, 1f - vRange.X);
                    uv.X += i * uStep;
                    uv.Y -= j * vStep;
                    uvs.Add(uv);
                }
            }

            Debug.Assert(mesh.Positions.Count == (horizontalCount + 1) * (verticalCount + 1));

            int rowLength = horizontalCount + 1;
            for (int j = 0; j < verticalCount; ++j)
            {
                for (int i = 0; i < horizontalCount; ++i)
                {
                    int[] index =
                    [
                        i + j * rowLength,
                        i + (j + 1) * rowLength,
                        i + 1 + j * rowLength,
                        i + 1 + (j + 1) * rowLength
                    ];

                    mesh.RawIndices.Add(index[0]);
                    mesh.RawIndices.Add(index[flipWinding ? 2 : 1]);
                    mesh.RawIndices.Add(index[flipWinding ? 1 : 2]);

                    mesh.RawIndices.Add(index[2]);
                    mesh.RawIndices.Add(index[flipWinding ? 3 : 1]);
                    mesh.RawIndices.Add(index[flipWinding ? 1 : 3]);
                }
            }

            var numIndices = 3 * 2 * horizontalCount * verticalCount;
            Debug.Assert(mesh.RawIndices.Count == numIndices);

            for (var i = 0; i < numIndices; ++i)
            {
                mesh.UVSets[0].Add(uvs[mesh.RawIndices[i]]);
            }

            return mesh;
        }

        private static void CreateCube(Scene scene, PrimitiveInitInfo info)
        {
        }

        private static void CreateUvSphere(Scene scene, PrimitiveInitInfo info)
        {
        }

        private static void CreateIcoSphere(Scene scene, PrimitiveInitInfo info)
        {
        }

        private static void CreateCylinder(Scene scene, PrimitiveInitInfo info)
        {
        }

        private static void CreateCapsule(Scene scene, PrimitiveInitInfo info)
        {
        }

        public static void CreatePrimitiveMesh(SceneData data, PrimitiveInitInfo info)
        {
            Debug.Assert(data != null && info != null);
            Debug.Assert(info.Type < PrimitiveMeshType.Count);

            var scene = new Scene();
            Creators[(int)info.Type](scene, info);

            data.Settings.CalculateNormals = true;
            ProcessScene(scene, data.Settings);
            PackData(scene, data);
        }

        private static void ProcessScene(Scene scene, GeometryImportSettings settings)
        {
        }

        private static void PackData(Scene scene, SceneData data)
        {
        }
    }
}
