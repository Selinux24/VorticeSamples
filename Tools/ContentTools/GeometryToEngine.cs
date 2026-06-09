using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;

namespace ContentTools
{
    public static class GeometryToEngine
    {
        public class Geometry()
        {
            public List<LODGroup> LODGroups { get; set; } = [];
        }
        public class LODGroup()
        {
            public string Name { get; set; }
            public List<MeshLOD> LODs { get; set; } = [];
        }
        public class MeshLOD
        {
            public string Name { get; set; }
            public float LodThreshold { get; set; }
            public List<Mesh> Meshes { get; set; } = [];
        }
        public class Mesh()
        {
            public static readonly int PositionSize = Marshal.SizeOf<Vector3>();

            public int ElementSize { get; set; }
            public int VertexCount { get; set; }
            public int IndexSize { get; set; }
            public int IndexCount { get; set; }
            public string Name { get; set; }
            public ElementsType ElementsType { get; set; }
            public PrimitiveTopology PrimitiveTopology { get; set; } = PrimitiveTopology.TriangleList;
            public byte[] Positions { get; set; }
            public byte[] Elements { get; set; }
            public byte[] Indices { get; set; }
        }

        public static Geometry ReadData(string assetFilename)
        {
            Geometry scene = new();

            BlobStreamReader blob = new(assetFilename);

            // skip scene name string
            int s = blob.Read<int>();
            blob.Skip(s);

            // get number of LODs
            int numLODGroups = blob.Read<int>();
            Debug.Assert(numLODGroups > 0);

            for (int i = 0; i < numLODGroups; i++)
            {
                // get LOD group's name
                s = blob.Read<int>();
                string lodGroupName;
                if (s > 0)
                {
                    lodGroupName = blob.ReadString(s);
                }
                else
                {
                    lodGroupName = $"lod_{Guid.NewGuid()}";
                }

                // get number of meshes in this LOD group
                int numMeshes = blob.Read<int>();
                Debug.Assert(numMeshes > 0);

                var lodGroup = new LODGroup()
                {
                    Name = lodGroupName,
                    LODs = ReadMeshLODs(numMeshes, blob),
                };

                scene.LODGroups.Add(lodGroup);
            }

            return scene;
        }
        private static List<MeshLOD> ReadMeshLODs(int numMeshes, BlobStreamReader blob)
        {
            var lodIds = new List<int>();
            var lodList = new List<MeshLOD>();

            for (int i = 0; i < numMeshes; ++i)
            {
                ReadMeshes(blob, lodIds, lodList);
            }

            return lodList;
        }
        private static void ReadMeshes(BlobStreamReader blob, List<int> lodIds, List<MeshLOD> lodList)
        {
            // get mesh's name
            var s = blob.Read<int>();
            string meshName;
            if (s > 0)
            {
                meshName = blob.ReadString(s);
            }
            else
            {
                meshName = $"mesh_{Guid.NewGuid()}";
            }

            var mesh = new Mesh() { Name = meshName };

            var lodId = blob.Read<int>();
            mesh.ElementSize = blob.Read<int>();
            mesh.ElementsType = (ElementsType)blob.Read<int>();
            mesh.PrimitiveTopology = PrimitiveTopology.TriangleList; // ContentTools currently only support triangle list meshes.
            mesh.VertexCount = blob.Read<int>();
            mesh.IndexSize = blob.Read<int>();
            mesh.IndexCount = blob.Read<int>();
            var lodThreshold = blob.Read<float>();

            var elementBufferSize = mesh.ElementSize * mesh.VertexCount;
            var indexBufferSize = mesh.IndexSize * mesh.IndexCount;

            mesh.Positions = blob.Read(Mesh.PositionSize * mesh.VertexCount);
            mesh.Elements = blob.Read(elementBufferSize);
            mesh.Indices = blob.Read(indexBufferSize);

            MeshLOD lod;
            if (lodId >= 0 && lodIds.Contains(lodId))
            {
                lod = lodList[lodIds.IndexOf(lodId)];
                Debug.Assert(lod != null);
            }
            else
            {
                lodIds.Add(lodId);
                lod = new MeshLOD()
                {
                    Name = meshName,
                    LodThreshold = lodThreshold
                };
                lodList.Add(lod);
            }

            lod.Meshes.Add(mesh);
        }

        public static void PackForEngine(Geometry scene, string fileName)
        {
            int sceneSize = GetSceneSizeForEngine(scene);
            IntPtr buffer = Marshal.AllocHGlobal(sceneSize);

            BlobStreamWriter blob = new(buffer, sceneSize);

            var lods = scene.LODGroups[0].LODs;

            // number of LODs
            blob.Write(lods.Count);
            lods.ForEach(lod => blob.Write(lod.LodThreshold));
            lods.ForEach(lod => blob.Write(lod.Meshes.Count));

            var sizeOfSubmeshesPosition = blob.Position;
            blob.Write(0);

            lods.ForEach(lod =>
            {
                lod.Meshes.ForEach(mesh =>
                {
                    PackMeshDataForEngine(mesh, blob);
                });
            });

            var endOfSubmeshes = blob.Position;
            var sizeOfSubmeshes = (int)(endOfSubmeshes - sizeOfSubmeshesPosition - sizeof(int));

            blob.Position = sizeOfSubmeshesPosition;
            blob.Write(sizeOfSubmeshes);
            blob.Position = endOfSubmeshes;

            lods.ForEach(lod =>
            {
                lod.Meshes.ForEach(mesh =>
                {
                    PackMeshForEngine(mesh, blob);
                });
            });

            Debug.Assert(sceneSize == blob.Offset);

            blob.SaveToFile(fileName);
        }
        private static int GetSceneSizeForEngine(Geometry scene)
        {
            int size = sizeof(int); // number of LODs

            foreach (var lod in scene.LODGroups[0].LODs)
            {
                int lodSize =
                    sizeof(float) +         // LOD threshold
                    sizeof(int) +           // number of meshes in this LOD
                    sizeof(int);            // size of submeshes

                foreach (var m in lod.Meshes)
                {
                    lodSize += GetMeshSizeForEngine(m);
                }

                size += lodSize;
            }

            return size;
        }
        private static int GetMeshSizeForEngine(Mesh m)
        {
            int positionBufferSize = (int)AlignSizeUp(m.Positions.Length, 4);
            int elementBufferSize = (int)AlignSizeUp(m.Elements.Length, 4);
            int indexBufferSize = (int)AlignSizeUp(m.Indices.Length, 4);

            int size =
                sizeof(int) +                               // vertex element size (vertex size excluding position element)
                sizeof(int) +                               // number of vertices
                sizeof(int) +                               // number of indices
                sizeof(uint) +                              // element type enumeration
                sizeof(uint) +                              // primitive topology
                positionBufferSize +                        // room for vertex positions
                elementBufferSize +                         // room for vertex elements
                indexBufferSize;                            // room for indices

            return size;
        }
        private static void PackMeshDataForEngine(Mesh mesh, BlobStreamWriter blob)
        {
            var alignedPositionBuffer = new byte[AlignSizeUp(mesh.Positions.Length, 4)];
            Array.Copy(mesh.Positions, alignedPositionBuffer, mesh.Positions.Length);
            var alignedElementBuffer = new byte[AlignSizeUp(mesh.Elements.Length, 4)];
            Array.Copy(mesh.Elements, alignedElementBuffer, mesh.Elements.Length);
            var alignedIndexBuffer = new byte[AlignSizeUp(mesh.Indices.Length, 4)];
            Array.Copy(mesh.Indices, alignedIndexBuffer, mesh.Indices.Length);

            blob.Write(alignedPositionBuffer);
            blob.Write(alignedElementBuffer);
            blob.Write(alignedIndexBuffer);
        }
        private static void PackMeshForEngine(Mesh mesh, BlobStreamWriter blob)
        {
            blob.Write(mesh.ElementSize);
            blob.Write(mesh.VertexCount);
            blob.Write(mesh.IndexCount);
            blob.Write((uint)mesh.ElementsType);
            blob.Write((uint)mesh.PrimitiveTopology);
        }
        private static long AlignSizeUp(long size, long alignment)
        {
            Debug.Assert(alignment > 0, "Alignment must be non-zero.");
            long mask = alignment - 1;
            Debug.Assert((alignment & mask) == 0, "Alignment should be a power of 2.");
            return (size + mask) & ~mask;
        }
    }
}
