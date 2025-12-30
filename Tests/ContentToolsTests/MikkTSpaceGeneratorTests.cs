using ContentTools.MikkTSpace;
using NUnit.Framework;
using System.Numerics;
using ContentTools;

namespace ContentToolsTests
{
    public class MikkTSpaceGeneratorTests
    {
        [Test()]
        public void GenerateTSpaceTri()
        {
            var mesh = TestMesh.GenTri();

            var generator = new TestMikkTSpaceGenerator(mesh);

            bool res = generator.GenTangSpace();
            Assert.That(res, Is.True);

            var resultTangents = TestMesh.GenTriResultTangets();

            for (int i = 0; i < resultTangents.Length; i++)
            {
                Assert.That(mesh.Faces[i].Tangent[0], Is.EqualTo(resultTangents[i].Tangent[0]));
                Assert.That(mesh.Faces[i].Tangent[1], Is.EqualTo(resultTangents[i].Tangent[1]));
                Assert.That(mesh.Faces[i].Tangent[2], Is.EqualTo(resultTangents[i].Tangent[2]));

                Assert.That(mesh.Faces[i].Sign[0], Is.EqualTo(resultTangents[i].Sign[0]));
                Assert.That(mesh.Faces[i].Sign[1], Is.EqualTo(resultTangents[i].Sign[1]));
                Assert.That(mesh.Faces[i].Sign[2], Is.EqualTo(resultTangents[i].Sign[2]));
            }
        }
        [Test()]
        public void GenerateTSpaceCube()
        {
            var mesh = TestMesh.GenCube();

            var generator = new TestMikkTSpaceGenerator(mesh);

            bool res = generator.GenTangSpace();
            Assert.That(res, Is.True);

            var resultTangents = TestMesh.GenCubeResultTangets();

            for (int i = 0; i < resultTangents.Length; i++)
            {
                Assert.That(mesh.Faces[i].Tangent[0], Is.EqualTo(resultTangents[i].Tangent[0]));
                Assert.That(mesh.Faces[i].Tangent[1], Is.EqualTo(resultTangents[i].Tangent[1]));
                Assert.That(mesh.Faces[i].Tangent[2], Is.EqualTo(resultTangents[i].Tangent[2]));

                Assert.That(mesh.Faces[i].Sign[0], Is.EqualTo(resultTangents[i].Sign[0]));
                Assert.That(mesh.Faces[i].Sign[1], Is.EqualTo(resultTangents[i].Sign[1]));
                Assert.That(mesh.Faces[i].Sign[2], Is.EqualTo(resultTangents[i].Sign[2]));
            }
        }
        [Test()]
        public void GenerateTSpaceBiPyramid()
        {
            var mesh = TestMesh.GenBiPyramid();

            var generator = new TestMikkTSpaceGenerator(mesh);

            bool res = generator.GenTangSpace();
            Assert.That(res, Is.True);

            var resultTangents = TestMesh.GenBiPyramidesultTangets();

            for (int i = 0; i < resultTangents.Length; i++)
            {
                Assert.That(Utils.NearEqual(mesh.Faces[i].Tangent[0], resultTangents[i].Tangent[0]), Is.True);
                Assert.That(Utils.NearEqual(mesh.Faces[i].Tangent[1], resultTangents[i].Tangent[1]), Is.True);
                Assert.That(Utils.NearEqual(mesh.Faces[i].Tangent[2], resultTangents[i].Tangent[2]), Is.True);

                Assert.That(mesh.Faces[i].Sign[0], Is.EqualTo(resultTangents[i].Sign[0]));
                Assert.That(mesh.Faces[i].Sign[1], Is.EqualTo(resultTangents[i].Sign[1]));
                Assert.That(mesh.Faces[i].Sign[2], Is.EqualTo(resultTangents[i].Sign[2]));
            }
        }
    }

    class TestFace
    {
        public Vector3[] Position;
        public Vector3[] Normal;
        public Vector2[] TexCoord;
        public Vector3[] Tangent;
        public float[] Sign;
    }

    class Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector3 Tangent;
        public float Sign;
    }

    static class Geom
    {
        public static readonly Vertex[] BiPyramidVertices =
        [
            new(){ Position = new(0.00000000f, 1.00000000f, 0.00000000f), Normal = new(0.00000000f, 1.00000000f, 0.00000000f), TexCoord = new(0.166666672f, 1.00000000f), Tangent = new(-0.866025329f, 0.00000000f, -0.500000000f), Sign = 1f},
            new(){ Position = new(0.00000000f, 1.00000000f, 0.00000000f), Normal = new(0.00000000f, 1.00000000f, 0.00000000f), TexCoord = new(0.500000000f, 1.00000000f), Tangent = new(0.00000000f, 0.00000000f, 1.00000000f), Sign = 1f},
            new(){ Position = new(0.00000000f, 1.00000000f, 0.00000000f), Normal = new(0.00000000f, 1.00000000f, 0.00000000f), TexCoord = new(0.833333313f, 1.00000000f), Tangent = new(0.866025329f, 0.00000000f, -0.500000000f), Sign = 1f},
            new(){ Position = new(0.999999881f, -1.19209290e-07f, -0.00000000f), Normal = new(1.00000000f, -1.33280025e-07f, 0.00000000f), TexCoord = new(0.00000000f, 0.500000000f), Tangent = new(0.00000000f, -2.30847760e-07f, -1.00000000f), Sign = 1f},
            new(){ Position = new(0.999999881f, -1.19209290e-07f, -0.00000000f), Normal = new(1.00000000f, -1.33280025e-07f, 0.00000000f), TexCoord = new(1.00000000f, 0.500000000f), Tangent = new(0.00000000f, 2.30847760e-07f, -1.00000000f), Sign = 1f},
            new(){ Position = new(-0.499999881f, -1.19209290e-07f, -0.866025329f), Normal = new(-0.500000000f, -1.49940035e-07f, -0.866025388f), TexCoord = new(0.333333343f, 0.500000000f), Tangent = new(-0.866025388f, -2.59703768e-07f, 0.500000000f), Sign = 1f},
            new(){ Position = new(-0.499999881f, -1.19209290e-07f, 0.866025329f), Normal = new(-0.500000000f, -1.49940050e-07f, 0.866025448f), TexCoord = new(0.666666687f, 0.500000000f), Tangent = new(0.866025269f, -2.59703796e-07f, 0.500000060f), Sign = 1f},
            new(){ Position = new(0.00000000f, -1.00000000f, 0.00000000f), Normal = new(0.00000000f, -1.00000000f, 0.00000000f), TexCoord = new(0.166666672f, 0.00000000f), Tangent = new(-0.866025329f, 0.00000000f, -0.500000000f), Sign = 1f},
            new(){ Position = new(0.00000000f, -1.00000000f, 0.00000000f), Normal = new(0.00000000f, -1.00000000f, 0.00000000f), TexCoord = new(0.500000000f, 0.00000000f), Tangent = new(0.00000000f, 0.00000000f, 1.00000000f), Sign = 1f},
            new(){ Position = new(0.00000000f, -1.00000000f, 0.00000000f), Normal = new(0.00000000f, -1.00000000f, 0.00000000f), TexCoord = new(0.833333313f, 0.00000000f), Tangent = new(0.866025329f, 0.00000000f, -0.500000000f), Sign = 1f},
        ];

        public static readonly uint[] BiPyramidIndices =
        [
            0,
            3,
            5,
            1,
            5,
            6,
            2,
            6,
            4,
            7,
            5,
            3,
            8,
            6,
            5,
            9,
            4,
            6,
        ];

        public static TestFace[] Build(uint[] indices, Vertex[] vertices)
        {
            TestFace[] faces = new TestFace[indices.Length / 3];

            for (int i = 0; i < indices.Length / 3; i++)
            {
                faces[i] = new TestFace()
                {
                    Position =
                    [
                        vertices[indices[i * 3 + 0]].Position,
                        vertices[indices[i * 3 + 1]].Position,
                        vertices[indices[i * 3 + 2]].Position,
                    ],
                    Normal =
                    [
                        vertices[indices[i * 3 + 0]].Normal,
                        vertices[indices[i * 3 + 1]].Normal,
                        vertices[indices[i * 3 + 2]].Normal,
                    ],
                    TexCoord =
                    [
                        vertices[indices[i * 3 + 0]].TexCoord,
                        vertices[indices[i * 3 + 1]].TexCoord,
                        vertices[indices[i * 3 + 2]].TexCoord,
                    ],
                };
            }

            return faces;
        }
        public static TestFace[] BuildResult(uint[] indices, Vertex[] vertices)
        {
            TestFace[] faces = new TestFace[indices.Length / 3];

            for (int i = 0; i < indices.Length / 3; i++)
            {
                faces[i] = new TestFace()
                {
                    Tangent =
                    [
                        vertices[indices[i * 3 + 0]].Tangent,
                        vertices[indices[i * 3 + 1]].Tangent,
                        vertices[indices[i * 3 + 2]].Tangent,
                    ],
                    Sign =
                    [
                        vertices[indices[i * 3 + 0]].Sign,
                        vertices[indices[i * 3 + 1]].Sign,
                        vertices[indices[i * 3 + 2]].Sign,
                    ],
                };
            }

            return faces;
        }
    }

    class TestMesh
    {
        public TestFace[] Faces;

        public static TestMesh GenTri()
        {
            TestFace[] faces =
            [
                // -X
                new() { Position=[new(-1, 1,-1), new(-1,-1,-1), new(-1, 1, 1)], Normal=[-Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX], TexCoord=[new(0, 0.625f), new(0f, 0.375f), new(0.25f, 0.625f)], },
            ];

            return new TestMesh() { Faces = faces };
        }
        public static TestFace[] GenTriResultTangets()
        {
            TestFace[] faces =
            [
                // -X
                new() { Tangent=[new( 0, 0, 1),new( 0, 0, 1),new( 0, 0, 1)], Sign=[ 1, 1, 1] },
            ];

            return faces;
        }

        public static TestMesh GenCube()
        {
            TestFace[] faces =
            [
                // -X
                new() { Position=[new(-1, 1,-1), new(-1,-1,-1), new(-1, 1, 1)], Normal=[-Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX], TexCoord=[new(0, 0.625f), new(0f, 0.375f), new(0.25f, 0.625f)], },
                new() { Position=[new(-1, 1, 1), new(-1,-1,-1), new(-1,-1, 1)], Normal=[-Vector3.UnitX,-Vector3.UnitX,-Vector3.UnitX], TexCoord=[new(0.25f, 0.625f), new(0f, 0.375f), new(0.25f, 0.375f)], },
               
                // +X
                new() { Position=[new( 1, 1, 1), new( 1,-1, 1), new( 1, 1,-1)], Normal=[ Vector3.UnitX, Vector3.UnitX, Vector3.UnitX], TexCoord=[new(0.5f, 0.625f), new(0.5f, 0.375f), new(0.75f, 0.625f)], },
                new() { Position=[new( 1, 1,-1), new( 1,-1, 1), new( 1,-1,-1)], Normal=[ Vector3.UnitX, Vector3.UnitX, Vector3.UnitX], TexCoord=[new(0.75f, 0.625f), new(0.5f, 0.375f), new(0.75f, 0.375f)], },
                
                // +Y
                new() { Position=[new(-1, 1,-1), new(-1, 1, 1), new( 1, 1,-1)], Normal=[ Vector3.UnitY, Vector3.UnitY, Vector3.UnitY], TexCoord=[new(0.25f, 0.875f), new(0.25f, 0.625f), new(0.5f, 0.875f)], },
                new() { Position=[new( 1, 1,-1), new(-1, 1, 1), new( 1, 1, 1)], Normal=[ Vector3.UnitY, Vector3.UnitY, Vector3.UnitY], TexCoord=[new(0.5f, 0.875f), new(0.25f, 0.625f), new(0.5f, 0.625f)], },
                
                // -Y
                new() { Position=[new(-1,-1, 1), new(-1,-1,-1), new( 1,-1, 1)], Normal=[-Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY], TexCoord=[new(0.25f, 0.375f), new(0.25f, 0.125f), new(0.5f, 0.375f)], },
                new() { Position=[new( 1,-1, 1), new(-1,-1,-1), new( 1,-1,-1)], Normal=[-Vector3.UnitY,-Vector3.UnitY,-Vector3.UnitY], TexCoord=[new(0.5f, 0.375f), new(0.25f, 0.125f), new(0.5f, 0.125f)], },
                
                // +Z
                new() { Position=[new(-1, 1, 1), new(-1,-1, 1), new( 1, 1, 1)], Normal=[ Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ], TexCoord=[new(0.25f, 0.625f), new(0.25f, 0.375f), new(0.5f, 0.625f)], },
                new() { Position=[new( 1, 1, 1), new(-1,-1, 1), new( 1,-1, 1)], Normal=[ Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ], TexCoord=[new(0.5f, 0.625f), new(0.25f, 0.375f), new(0.5f, 0.375f)], },
                
                // -Z
                new() { Position=[new( 1, 1,-1), new( 1,-1,-1), new(-1, 1,-1)], Normal=[-Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ], TexCoord=[new(0.75f, 0.625f), new(0.75f, 0.375f), new(1f, 0.625f)], },
                new() { Position=[new(-1, 1,-1), new( 1,-1,-1), new(-1,-1,-1)], Normal=[-Vector3.UnitZ,-Vector3.UnitZ,-Vector3.UnitZ], TexCoord=[new(1, 0.625f), new(0.75f, 0.375f), new(1f, 0.375f)], },
            ];

            return new TestMesh() { Faces = faces };
        }
        public static TestFace[] GenCubeResultTangets()
        {
            TestFace[] faces =
            [
                // -X
                new() { Tangent=[new( 0, 0, 1),new( 0, 0, 1),new( 0, 0, 1)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new( 0, 0, 1),new( 0, 0, 1),new( 0, 0, 1)], Sign=[ 1, 1, 1] },

                // +X
                new() { Tangent=[new( 0, 0,-1),new( 0, 0,-1),new( 0, 0,-1)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new( 0, 0,-1),new( 0, 0,-1),new( 0, 0,-1)], Sign=[ 1, 1, 1] },

                // +Y
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },

                // -Y
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },

                // +Z
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new( 1, 0, 0),new( 1, 0, 0),new( 1, 0, 0)], Sign=[ 1, 1, 1] },

                // -Z
                new() { Tangent=[new(-1, 0, 0),new(-1, 0, 0),new(-1, 0, 0)], Sign=[ 1, 1, 1] },
                new() { Tangent=[new(-1, 0, 0),new(-1, 0, 0),new(-1, 0, 0)], Sign=[ 1, 1, 1] },
            ];

            return faces;
        }

        public static TestMesh GenBiPyramid()
        {
            return new TestMesh() { Faces = Geom.Build(Geom.BiPyramidIndices, Geom.BiPyramidVertices) };
        }
        public static TestFace[] GenBiPyramidesultTangets()
        {
            return Geom.BuildResult(Geom.BiPyramidIndices, Geom.BiPyramidVertices);
        }
    }

    class TestMikkTSpaceGenerator(TestMesh mesh) : MikkTSpaceGenerator<TestMesh>(mesh)
    {
        public override int GetNumFaces()
        {
            return GetMesh().Faces.Length;
        }
        public override int GetNumVerticesOfFace(int faceIndex)
        {
            return 3;
        }
        public override Vector3 GetPosition(int faceIndex, int vertIndex)
        {
            return GetMesh().Faces[faceIndex].Position[vertIndex];
        }
        public override Vector3 GetNormal(int faceIndex, int vertIndex)
        {
            return GetMesh().Faces[faceIndex].Normal[vertIndex];
        }
        public override Vector2 GetTexCoord(int faceIndex, int vertIndex)
        {
            return GetMesh().Faces[faceIndex].TexCoord[vertIndex];
        }

        public override void SetTSpaceBasic(Vector3 tangent, float sign, int faceIndex, int vertIndex)
        {
            var face = GetMesh().Faces[faceIndex];
            face.Tangent ??= new Vector3[3];
            face.Sign ??= new float[3];

            face.Tangent[vertIndex] = tangent;
            face.Sign[vertIndex] = sign;
        }
    }
}
