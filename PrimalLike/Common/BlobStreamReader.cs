using System;
using System.Runtime.InteropServices;

namespace PrimalLike.Common
{
    public class BlobStreamReader(IntPtr data)
    {
        private readonly IntPtr data = data;
        private int position = 0;

        public uint ReadUInt32()
        {
            uint value = (uint)Marshal.ReadInt32(data, position);
            position += sizeof(uint);
            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(data, position)), 0);
            position += sizeof(float);
            return value;
        }

        public void Skip(int bytes)
        {
            position += bytes;
        }

        public byte[] ReadBytes(int totalBufferSize)
        {
            byte[] bytes = new byte[totalBufferSize];
            Marshal.Copy(data + position, bytes, 0, totalBufferSize);
            position += totalBufferSize;
            return bytes;
        }

        public int Position => position;
    }
}
