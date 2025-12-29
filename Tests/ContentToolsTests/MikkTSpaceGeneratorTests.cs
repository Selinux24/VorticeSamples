using ContentTools.MikkTSpace;
using NUnit.Framework;
using System.Numerics;

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
    }

    class TestFace
    {
        public Vector3[] Position;
        public Vector3[] Normal;
        public Vector2[] TexCoord;
        public Vector3[] Tangent;
        public float[] Sign;
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
