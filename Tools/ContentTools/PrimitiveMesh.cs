using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace ContentTools
{
    public static class PrimitiveMesh
    {
        const int AxisX = 0;
        const int AxisY = 1;
        const int AxisZ = 2;

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
            var lod = new LODGroup { Name = "plane", Meshes = [CreatePlane(info)] };
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreatePlane(PrimitiveInitInfo info, int horizontalIndex = AxisX, int verticalIndex = AxisZ, bool flipWinding = false, Vector3 offset = default, Vector2 uRange = default, Vector2 vRange = default)
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

        private static Vector3 GetFaceVertex(uint face, float x, float y)
        {
            Vector3[] faceVertex =
            [
                new (-1f,  -y,   x), // X- Right
                new ( 1f,  -y,  -x), // X+ Left
                new (  x,  1f,   y), // Y+ Bottom
                new (  x, -1f,  -y), // Y- Top
                new (  x,  -y,  1f), // Z+ Front
                new ( -x,  -y, -1f), // Z- Back
            ];

            return faceVertex[face];
        }

        private static void CreateCube(Scene scene, PrimitiveInitInfo info)
        {
            var lod = new LODGroup { Name = "cube", Meshes = [CreateCube(info)] };
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreateCube(PrimitiveInitInfo info)
        {
            uint[] segments = info.Segments;
            int[][] axes = [[AxisZ, AxisY], [AxisX, AxisZ], [AxisX, AxisY]];
            float[] uRange = [0f, 0.5f, 0.25f, 0.25f, 0.25f, 0.75f];
            float[] vRange = [0.375f, 0.375f, 0.125f, 0.625f, 0.375f, 0.375f];
            List<Vector3> positions = [];
            List<uint> rawIndices = [];
            List<Vector2> uvs = [];

            for (uint face = 0; face < 6; face++)
            {
                uint axesIndex = face >> 1;
                var axis = axes[axesIndex];
                uint xCount = Math.Clamp(segments[axis[0]], 1, 10);
                uint yCount = Math.Clamp(segments[axis[1]], 1, 10);
                float xStep = 1f / xCount;
                float yStep = 1f / yCount;
                float uStep = 0.25f / xCount;
                float vStep = 0.25f / yCount;

                uint rawIndexOffset = (uint)positions.Count;

                for (uint y = 0; y <= yCount; y++)
                {
                    for (uint x = 0; x <= xCount; x++)
                    {
                        var pos = new Vector2(2f * x * xStep - 1f, 2f * y * yStep - 1f);
                        var position = GetFaceVertex(face, pos.X, pos.Y);
                        positions.Add(new(position.X * info.Size.X, position.Y * info.Size.Y, position.Z * info.Size.Z));
#if true
                        var uv = new Vector2(uRange[face], 1f - vRange[face]);
                        uv.X += x * uStep;
                        uv.Y -= y * vStep;
#else
                        var uv = new Vector2( 0f, 1f )                        ;
                        uv.X += x % 2;
                        uv.Y -= y % 2;
#endif
                        uvs.Add(uv);
                    }
                }

                uint rowLength = xCount + 1; // number of vertices in a row
                for (uint y = 0; y < yCount; y++)
                {
                    for (uint x = 0; x < xCount; x++)
                    {
                        uint[] index =
                        [
                            rawIndexOffset + x + y * rowLength,
                            rawIndexOffset + x + (y + 1) * rowLength,
                            rawIndexOffset + (x + 1) + y * rowLength,
                            rawIndexOffset + (x + 1) + (y + 1) * rowLength
                        ];

                        rawIndices.Add(index[0]);
                        rawIndices.Add(index[1]);
                        rawIndices.Add(index[2]);

                        rawIndices.Add(index[2]);
                        rawIndices.Add(index[1]);
                        rawIndices.Add(index[3]);
                    }
                }
            }

            List<Vector2> uvSet = [];
            for (int i = 0; i < rawIndices.Count; i++)
            {
                uvSet.Add(uvs[(int)rawIndices[i]]);
            }

            return new()
            {
                Positions = [.. positions],
                RawIndices = [.. rawIndices],
                UVSets = [[.. uvSet]]
            };
        }

        private static void CreateUvSphere(Scene scene, PrimitiveInitInfo info)
        {
            var lod = new LODGroup { Name = "uv-sphere", Meshes = [CreateUvSphere(info)] };
            scene.LODGroups.Add(lod);
        }
        private static Mesh CreateUvSphere(PrimitiveInitInfo info)
        {
            uint phiCount = Math.Clamp(info.Segments[AxisX], 3, 64);
            uint thetaCount = Math.Clamp(info.Segments[AxisY], 2, 64);
            float thetaStep = MathF.PI / thetaCount;
            float phiStep = 2 * MathF.PI / phiCount;
            uint numIndices = 2 * 3 * phiCount + 2 * 3 * phiCount * (thetaCount - 2);
            uint numVertices = 2 + phiCount * (thetaCount - 1);

            Vector3[] positions = new Vector3[numVertices];

            // Add the top vertex
            uint c = 0;
            positions[c++] = new Vector3(0f, info.Size.Y, 0f);

            for (uint j = 1; j <= (thetaCount - 1); j++)
            {
                float theta = j * thetaStep;
                for (uint i = 0; i < phiCount; ++i)
                {
                    float phi = i * phiStep;
                    positions[c++] = new Vector3(
                        info.Size.X * MathF.Sin(theta) * MathF.Cos(phi),
                        info.Size.Y * MathF.Cos(theta),
                        -info.Size.Z * MathF.Sin(theta) * MathF.Sin(phi));
                }
            }

            // Add the bottom vertex
            positions[c++] = new Vector3(0f, -info.Size.Y, 0f);
            Debug.Assert(c == numVertices);

            c = 0;
            uint[] rawIndices = new uint[numIndices];
            Vector2[] uvs = new Vector2[numIndices];
            float invThetaCount = 1f / thetaCount;
            float invPhiCount = 1f / phiCount;

            // Indices for the top cap, connecting the north pole to the first ring
            for (uint i = 0; i < phiCount - 1; ++i)
            {
                uvs[c] = new Vector2((2 * i + 1) * 0.5f * invPhiCount, 1f);
                rawIndices[c++] = 0;
                uvs[c] = new Vector2(i * invPhiCount, 1f - invThetaCount);
                rawIndices[c++] = i + 1;
                uvs[c] = new Vector2((i + 1) * invPhiCount, 1f - invThetaCount);
                rawIndices[c++] = i + 2;
            }

            uvs[c] = new Vector2(1f - 0.5f * invPhiCount, 1f);
            rawIndices[c++] = 0;
            uvs[c] = new Vector2(1f - invPhiCount, 1f - invThetaCount);
            rawIndices[c++] = phiCount;
            uvs[c] = new Vector2(1f, 1f - invThetaCount);
            rawIndices[c++] = 1;

            // Indices for the section between the top and bottom rings
            for (uint j = 0; j < (thetaCount - 2); j++)
            {
                uint[] index;

                for (uint i = 0; i < (phiCount - 1); i++)
                {
                    index =
                    [
                        1 + i + j * phiCount,
                        1 + i + (j + 1) * phiCount,
                        1 + (i + 1) + (j + 1) * phiCount,
                        1 + (i + 1) + j * phiCount
                    ];

                    uvs[c] = new Vector2(i * invPhiCount, 1f - (j + 1) * invThetaCount);
                    rawIndices[c++] = index[0];
                    uvs[c] = new Vector2(i * invPhiCount, 1f - (j + 2) * invThetaCount);
                    rawIndices[c++] = index[1];
                    uvs[c] = new Vector2((i + 1) * invPhiCount, 1f - (j + 2) * invThetaCount);
                    rawIndices[c++] = index[2];

                    uvs[c] = new Vector2(i * invPhiCount, 1f - (j + 1) * invThetaCount);
                    rawIndices[c++] = index[0];
                    uvs[c] = new Vector2((i + 1) * invPhiCount, 1f - (j + 2) * invThetaCount);
                    rawIndices[c++] = index[2];
                    uvs[c] = new Vector2((i + 1) * invPhiCount, 1f - (j + 1) * invThetaCount);
                    rawIndices[c++] = index[3];
                }

                index =
                [
                    phiCount + j * phiCount,
                    phiCount + (j + 1) * phiCount,
                    1 + (j + 1) * phiCount,
                    1 + j * phiCount,
                ];

                uvs[c] = new Vector2(1f - invPhiCount, 1f - (j + 1) * invThetaCount);
                rawIndices[c++] = index[0];
                uvs[c] = new Vector2(1f - invPhiCount, 1f - (j + 2) * invThetaCount);
                rawIndices[c++] = index[1];
                uvs[c] = new Vector2(1f, 1f - (j + 2) * invThetaCount);
                rawIndices[c++] = index[2];

                uvs[c] = new Vector2(1f - invPhiCount, 1f - (j + 1) * invThetaCount);
                rawIndices[c++] = index[0];
                uvs[c] = new Vector2(1f, 1f - (j + 2) * invThetaCount);
                rawIndices[c++] = index[2];
                uvs[c] = new Vector2(1f, 1f - (j + 1) * invThetaCount);
                rawIndices[c++] = index[3];
            }

            // Indices for the bottom cap, connecting the south posle to the last ring
            uint southPoleIndex = (uint)positions.Length - 1;
            for (uint i = 0; i < (phiCount - 1); i++)
            {
                uvs[c] = new Vector2((2 * i + 1) * 0.5f * invPhiCount, 0f);
                rawIndices[c++] = southPoleIndex;
                uvs[c] = new Vector2((i + 1) * invPhiCount, invThetaCount);
                rawIndices[c++] = southPoleIndex - phiCount + i + 1;
                uvs[c] = new Vector2(i * invPhiCount, invThetaCount);
                rawIndices[c++] = southPoleIndex - phiCount + i;
            }

            uvs[c] = new Vector2(1f - 0.5f * invPhiCount, 0f);
            rawIndices[c++] = southPoleIndex;
            uvs[c] = new Vector2(1f, invThetaCount);
            rawIndices[c++] = southPoleIndex - phiCount;
            uvs[c] = new Vector2(1f - invPhiCount, invThetaCount);
            rawIndices[c++] = southPoleIndex - 1;

            Debug.Assert(c == numIndices);

            Mesh m = new()
            {
                Name = "uv_sphere",
                Positions = positions,
                RawIndices = rawIndices,
                UVSets = [uvs],
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

        public static IEnumerable<string> CreatePrimitiveMesh(GeometryImportSettings settings, PrimitiveInitInfo info, string destinationFolder = null)
        {
            Debug.Assert(settings != null && info != null);
            Debug.Assert(info.Type < PrimitiveMeshType.Count);

            var scene = new Scene("PrimitiveMesh");
            Creators[(int)info.Type](scene, info);

            Geometry.ProcessScene(scene, settings);

            yield return Assets.Create(scene, destinationFolder ?? Path.GetRandomFileName());
        }
    }
}
