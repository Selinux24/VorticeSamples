using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            var meshLOD = new MeshLOD { Name = "plane", Threshold = -1f, Meshes = [CreatePlane(info)] };
            var lod = new LODGroup { Name = "plane", LODs = [meshLOD] };
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreatePlane(PrimitiveInitInfo info, int horizontalIndex = 0, int verticalIndex = 2, bool flipWinding = false, Vector3 offset = default, Vector2 uRange = default, Vector2 vRange = default)
        {
            Debug.Assert(horizontalIndex < 3 && verticalIndex < 3);
            Debug.Assert(horizontalIndex != verticalIndex);

            if (offset == default) offset = new Vector3(-0.5f, 0f, -0.5f);
            if (uRange == default) uRange = new Vector2(0f, 1f);
            if (vRange == default) vRange = new Vector2(0f, 1f);

            uint horizontalCount = Math.Clamp(info.Segments[horizontalIndex], 1, 10);
            uint verticalCount = Math.Clamp(info.Segments[verticalIndex], 1, 10);
            float horizontalStep = 1f / horizontalCount;
            float verticalStep = 1f / verticalCount;
            float uStep = (uRange.Y - uRange.X) / horizontalCount;
            float vStep = (vRange.Y - vRange.X) / verticalCount;

            var positions = new List<Vector3>();
            var rawIndices = new List<uint>();
            var uvSets = new List<List<Vector2>>();
            var uvs = new List<Vector2>();

            for (uint j = 0; j <= verticalCount; ++j)
            {
                for (uint i = 0; i <= horizontalCount; ++i)
                {
                    var position = offset;
                    var asArray = new[] { position.X, position.Y, position.Z };
                    asArray[horizontalIndex] += i * horizontalStep;
                    asArray[verticalIndex] += j * verticalStep;
                    positions.Add(new Vector3(asArray[0] * info.Size.X, asArray[1] * info.Size.Y, asArray[2] * info.Size.Z));

                    var uv = new Vector2(uRange.X, 1f - vRange.X);
                    uv.X += i * uStep;
                    uv.Y -= j * vStep;
                    uvs.Add(uv);
                }
            }

            Debug.Assert(positions.Count == (horizontalCount + 1) * (verticalCount + 1));

            uint rowLength = horizontalCount + 1;
            for (uint j = 0; j < verticalCount; ++j)
            {
                for (uint i = 0; i < horizontalCount; ++i)
                {
                    uint[] index =
                    [
                        i + j * rowLength,
                        i + (j + 1) * rowLength,
                        i + 1 + j * rowLength,
                        i + 1 + (j + 1) * rowLength
                    ];

                    rawIndices.Add(index[0]);
                    rawIndices.Add(index[flipWinding ? 2 : 1]);
                    rawIndices.Add(index[flipWinding ? 1 : 2]);

                    rawIndices.Add(index[2]);
                    rawIndices.Add(index[flipWinding ? 3 : 1]);
                    rawIndices.Add(index[flipWinding ? 1 : 3]);
                }
            }

            uint numIndices = 3 * 2 * horizontalCount * verticalCount;
            Debug.Assert(rawIndices.Count == numIndices);

            for (uint i = 0; i < numIndices; ++i)
            {
                int rawIndex = (int)rawIndices[(int)i];
                uvSets[0].Add(uvs[rawIndex]);
            }

            var mesh = new Mesh()
            {
                Name = "plane",
                Positions = [.. positions],
                RawIndices = [.. rawIndices],
                UVSets = [.. uvSets.Select(set => set.ToArray())],
            };

            return mesh;
        }

        private static void CreateCube(Scene scene, PrimitiveInitInfo info)
        {
        }

        private static void CreateUvSphere(Scene scene, PrimitiveInitInfo info)
        {
            var meshLOD = new MeshLOD { Name = "uv-sphere", Threshold = -1f, Meshes = [CreateUvSphere(info)] };
            var lod = new LODGroup { Name = "uv-sphere", LODs = [meshLOD] };
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreateUvSphere(PrimitiveInitInfo info)
        {
            uint phiCount = Math.Clamp(info.Segments[0], 3, 64);
            uint thetaCount = Math.Clamp(info.Segments[1], 2, 64);
            float thetaStep = MathF.PI / thetaCount;
            float phiStep = 2 * MathF.PI / phiCount;
            uint numIndices = 2 * 3 * phiCount + 2 * 3 * phiCount * (thetaCount - 2);
            uint numVertices = 2 + phiCount * (thetaCount - 1);

            List<Vector3> positions = [];
            uint c = 0;
            positions[(int)c++] = new Vector3(0f, info.Size.Y, 0f);

            for (uint j = 1; j <= (thetaCount - 1); ++j)
            {
                float theta = j * thetaStep;
                for (uint i = 0; i < phiCount; ++i)
                {
                    float phi = i * phiStep;
                    positions[(int)c++] = new Vector3(
                        info.Size.X * MathF.Sin(theta) * MathF.Cos(phi),
                        info.Size.Y * MathF.Cos(theta),
                        -info.Size.Z * MathF.Sin(theta) * MathF.Sin(phi));
                }
            }

            positions[(int)c++] = new Vector3(0f, -info.Size.Y, 0f);
            Debug.Assert(c == numVertices);

            c = 0;
            List<uint> rawIndices = new(new uint[numIndices]);
            List<Vector2> uvs = new(new Vector2[numIndices]);
            float invThetaCount = 1f / thetaCount;
            float invPhiCount = 1f / phiCount;

            for (uint i = 0; i < phiCount - 1; ++i)
            {
                uvs[(int)c] = new Vector2((2 * i + 1) * 0.5f * invPhiCount, 1f);
                rawIndices[(int)c++] = 0;
                uvs[(int)c] = new Vector2(i * invPhiCount, 1f - invThetaCount);
            }

            Mesh m = new()
            {
                Name = "uv_sphere",
                Positions = [.. positions],
                RawIndices = [.. rawIndices],
            };

            return m;
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

            var scene = new Scene("PrimitiveMesh");
            Creators[(int)info.Type](scene, info);

            data.Settings.CalculateNormals = true;
            Geometry.ProcessScene(scene, data.Settings);
            Geometry.PackForEditor(scene, data);
        }
    }
}
