using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ContentTools
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PackedVertex
    {
        public Vector3 Position;
        public byte Signs;
        public ushort NormalX;
        public ushort NormalY;
        public Vector2 UV;

        public PackedVertex(Vertex v)
        {
            byte signs = (byte)((v.Normal.Z > 0f) ? 2 : 0);
            PackFloat(v.Normal.X, -1f, 1f, out ushort normalX);
            PackFloat(v.Normal.Y, -1f, 1f, out ushort normalY);

            Position = v.Position;
            Signs = signs;
            NormalX = normalX;
            NormalY = normalY;
            UV = v.UV;
        }
        private static void PackFloat(float f, float min, float max, out ushort pv)
        {
            float value = (f - min) / (max - min);
            const float intervals = (1 << 16) - 1;
            pv = (ushort)(intervals * value + 0.5f);
        }

        public readonly byte[] GetData()
        {
            byte[] pX = BitConverter.GetBytes(Position.X);
            byte[] pY = BitConverter.GetBytes(Position.Y);
            byte[] pZ = BitConverter.GetBytes(Position.Z);
            byte[] s = [Signs, 0, 0, 0];
            byte[] nX = BitConverter.GetBytes(NormalX);
            byte[] nY = BitConverter.GetBytes(NormalY);
            byte[] uvX = BitConverter.GetBytes(UV.X);
            byte[] uvY = BitConverter.GetBytes(UV.Y);

            return
            [
                ..pX,
                ..pY,
                ..pZ,
                ..s,
                ..nX,
                ..nY,
                ..uvX,
                ..uvY,
            ];
        }
    }
}
