using System.Numerics;

namespace ContentTools
{
    public class PrimitiveInitInfo
    {
        public PrimitiveMeshType Type { get; set; }
        public uint[] Segments { get; set; } = [1, 1, 1];
        public Vector3 Size { get; set; } = new Vector3(1, 1, 1);
        public uint Lod { get; set; }
    }
}
