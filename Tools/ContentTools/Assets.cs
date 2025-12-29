using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Utilities;

namespace ContentTools
{
    public static class Assets
    {
        public static string Create(Scene scene, string destinationFolder)
        {
            int sceneSize = GetSceneSize(scene);
            IntPtr buffer = Marshal.AllocHGlobal(sceneSize);

            BlobStreamWriter blob = new(buffer, sceneSize);

            // scene name
            blob.Write(scene.Name);

            // number of LODs
            blob.Write(scene.LODGroups.Count);

            foreach (var lod in scene.LODGroups)
            {
                // LOD name
                blob.Write(lod.Name);

                // number of meshes in this LOD
                blob.Write(lod.Meshes.Count);

                foreach (var m in lod.Meshes)
                {
                    PackMesh(m, blob);
                }
            }

            Debug.Assert(sceneSize == blob.Offset);

            string fileName = Path.Combine(destinationFolder, Path.ChangeExtension(scene.Name, ".asset"));
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            blob.SaveToFile(fileName);

            return fileName;
        }
        public static string Create(LODGroup lodGroup, string destinationFolder)
        {
            int sceneSize = GetLODGroupSize(lodGroup);
            IntPtr buffer = Marshal.AllocHGlobal(sceneSize);

            BlobStreamWriter blob = new(buffer, sceneSize);

            // scene name
            blob.Write(lodGroup.Name);

            // number of LODs
            blob.Write(1);

            // LOD name
            blob.Write(lodGroup.Name);

            // number of meshes in this LOD
            blob.Write(lodGroup.Meshes.Count);

            foreach (var m in lodGroup.Meshes)
            {
                PackMesh(m, blob);
            }

            Debug.Assert(sceneSize == blob.Offset);

            string fileName = Path.Combine(destinationFolder, Path.ChangeExtension(lodGroup.Name, ".asset"));
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            blob.SaveToFile(fileName);

            return fileName;
        }
        static int GetSceneSize(Scene scene)
        {
            int size;
            int sceneNameLength = scene.Name.Length;

            size =
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            foreach (var lod in scene.LODGroups)
            {
                int lodSize;
                int lodNameLength = lod.Name.Length;

                lodSize =
                    sizeof(int) + lodNameLength +   // LOD name length and room for LOD name string
                    sizeof(int);                    // number of meshes in this LOD

                foreach (var m in lod.Meshes)
                {
                    lodSize += GetMeshSize(m);
                }

                size += lodSize;
            }

            return size;
        }
        static int GetLODGroupSize(LODGroup lodGroup)
        {
            int size;
            int sceneNameLength = lodGroup.Name.Length;

            size =
                sizeof(int) +               // name length
                sceneNameLength +           // room for scene name string
                sizeof(int);                // number of LODs

            int lodSize;
            int lodNameLength = lodGroup.Name.Length;

            lodSize =
                sizeof(int) + lodNameLength +   // LOD name length and room for LOD name string
                sizeof(int);                    // number of meshes in this LOD

            foreach (var m in lodGroup.Meshes)
            {
                lodSize += GetMeshSize(m);
            }

            size += lodSize;

            return size;
        }
        static int GetMeshSize(Mesh m)
        {
            int nameLength = m.Name.Length;
            int numVertices = m.Vertices.Count;
            int numIndices = m.Indices.Count;
            int positionBufferSize = m.PositionBuffer.Length;
            Debug.Assert(positionBufferSize == Marshal.SizeOf<Vector3>() * numVertices);
            int elementBufferSize = m.ElementBuffer.Length;
            Debug.Assert(elementBufferSize == PackingHelper.GetVertexElementsSize(m.ElementsType) * numVertices);
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            int indexBufferSize = indexSize * numIndices;

            int size =
                sizeof(int) + nameLength +      // mesh name length and room for mesh name string
                sizeof(uint) +                  // lod id
                sizeof(int) +                   // vertex element size (vertex size excluding position element)
                sizeof(int) +                   // element type enumeration
                sizeof(int) +                   // number of vertices
                sizeof(int) +                   // index size (16 bit or 32 bit)
                sizeof(int) +                   // number of indices
                sizeof(float) +                 // LOD threshold
                positionBufferSize +            // room for vertex positions
                elementBufferSize +             // room for vertex elements
                indexBufferSize;                // room for indices

            return size;
        }
        static void PackMesh(Mesh m, BlobStreamWriter blob)
        {
            // mesh name
            blob.Write(m.Name);

            // lod id
            blob.Write(m.LodId);

            // elements size
            int elementsSize = PackingHelper.GetVertexElementsSize(m.ElementsType);
            blob.Write(elementsSize);

            // elements type enumeration
            blob.Write((uint)m.ElementsType);

            // number of vertices
            int numVertices = m.Vertices.Count;
            blob.Write(numVertices);

            // index size (16 bit or 32 bit)
            int indexSize = (numVertices < (1 << 16)) ? sizeof(ushort) : sizeof(uint);
            blob.Write(indexSize);

            // number of indices
            int numIndices = m.Indices.Count;
            blob.Write(numIndices);

            // LOD threshold
            blob.Write(m.LodThreshold);

            // position buffer
            Debug.Assert(m.PositionBuffer.Length == Marshal.SizeOf<Vector3>() * numVertices);
            blob.Write(m.PositionBuffer);

            // element buffer
            Debug.Assert(m.ElementBuffer.Length == elementsSize * numVertices);
            blob.Write(m.ElementBuffer);

            // index data
            int indexDataLenght = indexSize * numIndices;
            if (indexSize == (uint)sizeof(ushort))
            {
                ushort[] data = [.. m.Indices.Select(i => (ushort)i)];
                blob.Write(data);
            }
            else
            {
                uint[] data = [.. m.Indices];
                blob.Write(data);
            }
        }
    }
}
