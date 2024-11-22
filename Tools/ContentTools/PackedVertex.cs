using System.Numerics;

namespace ContentTools
{
    public class PackedVertex
    {
        public Vector3 Position { get; set; }
        public byte Signs { get; set; }
        public ushort NormalX { get; set; }
        public ushort NormalY { get; set; }
        public Vector2 UV { get; set; }
    }
}
