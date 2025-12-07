using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ContentTools
{
    static class PackingHelper
    {
        private static float PackFloat(uint bits, float f, float min, float max)
        {
            float value = (f - min) / (max - min);
            float intervals = (1u << (int)bits) - 1;
            return intervals * value + 0.5f;
        }
        private static float PackUnitFloat(uint bits, float f)
        {
            float intervals = (1u << (int)bits) - 1;
            return intervals * f + 0.5f;
        }

        public static ushort PackFloat16(float f, float min, float max)
        {
            return (ushort)PackFloat(16, f, min, max);
        }
        public static byte PackUnitFloat8(float f)
        {
            return (byte)PackUnitFloat(8, f);
        }

        public static byte PackTSign(Vector3 normal)
        {
            return (byte)((normal.Z > 0f ? 1u : 0u) << 2);
        }
        public static byte PackTSign(Vector4 tangent)
        {
            return (byte)(((tangent.W > 0f) ? 1u : 0u) | (((tangent.Z > 0f) ? 1u : 0u) << 1));
        }

        public static int GetVertexElementsSize(ElementsType type)
        {
            return type switch
            {
                ElementsType.StaticNormal => StaticNormal.GetStride(),
                ElementsType.StaticNormalTexture => StaticNormalTexture.GetStride(),
                ElementsType.StaticColor => StaticColor.GetStride(),
                ElementsType.Skeletal => Skeletal.GetStride(),
                ElementsType.SkeletalColor => SkeletalColor.GetStride(),
                ElementsType.SkeletalNormal => SkeletalNormal.GetStride(),
                ElementsType.SkeletalNormalColor => SkeletalNormalColor.GetStride(),
                ElementsType.SkeletalNormalTexture => SkeletalNormalTexture.GetStride(),
                ElementsType.SkeletalNormalTextureColor => SkeletalNormalTextureColor.GetStride(),
                _ => 0
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct StaticColor()
    {
        [FieldOffset(0)]
        public byte Red;
        [FieldOffset(1)]
        public byte Green;
        [FieldOffset(2)]
        public byte Blue;
        [FieldOffset(3)]
        public byte Padding;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(StaticColor));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(0);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct StaticNormal()
    {
        [FieldOffset(0)]
        public byte Red;
        [FieldOffset(1)]
        public byte Green;
        [FieldOffset(2)]
        public byte Blue;
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public ushort NormalX;
        [FieldOffset(6)]
        public ushort NormalY;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(StaticNormal));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(PackingHelper.PackTSign(vertex.Normal));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct StaticNormalTexture()
    {
        [FieldOffset(0)]
        public byte Red;
        [FieldOffset(1)]
        public byte Green;
        [FieldOffset(2)]
        public byte Blue;
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public ushort NormalX;
        [FieldOffset(6)]
        public ushort NormalY;
        [FieldOffset(8)]
        public ushort TangentX;
        [FieldOffset(10)]
        public ushort TangentY;
        [FieldOffset(12)]
        public Vector2 Uv;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(StaticNormalTexture));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(PackingHelper.PackTSign(vertex.Tangent));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(vertex.UV.X));
            ms.Write(BitConverter.GetBytes(vertex.UV.Y));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct Skeletal()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte Padding;
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(Skeletal));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(0);
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SkeletalColor()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte Padding1;
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;
        [FieldOffset(20)]
        public byte Red;
        [FieldOffset(21)]
        public byte Green;
        [FieldOffset(22)]
        public byte Blue;
        [FieldOffset(23)]
        public byte Padding2;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(SkeletalColor));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(0);
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(0);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SkeletalNormal()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;
        [FieldOffset(20)]
        public ushort NormalX;
        [FieldOffset(22)]
        public ushort NormalY;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(SkeletalNormal));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(PackingHelper.PackTSign(vertex.Normal));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SkeletalNormalColor()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;
        [FieldOffset(20)]
        public ushort NormalX;
        [FieldOffset(22)]
        public ushort NormalY;
        [FieldOffset(24)]
        public byte Red;
        [FieldOffset(25)]
        public byte Green;
        [FieldOffset(26)]
        public byte Blue;
        [FieldOffset(27)]
        public byte Padding;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(SkeletalNormalColor));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(PackingHelper.PackTSign(vertex.Normal));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(0);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SkeletalNormalTexture()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;
        [FieldOffset(20)]
        public ushort NormalX;
        [FieldOffset(22)]
        public ushort NormalY;
        [FieldOffset(24)]
        public ushort TangentX;
        [FieldOffset(26)]
        public ushort TangentY;
        [FieldOffset(28)]
        public Vector2 Uv;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(SkeletalNormalTexture));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(PackingHelper.PackTSign(vertex.Tangent));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(vertex.UV.X));
            ms.Write(BitConverter.GetBytes(vertex.UV.Y));
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SkeletalNormalTextureColor()
    {
        [FieldOffset(0)]
        public byte JointWeights0; // normalized joint weights for up to 4 joints.
        [FieldOffset(1)]
        public byte JointWeights1; // normalized joint weights for up to 4 joints.
        [FieldOffset(2)]
        public byte JointWeights2; // normalized joint weights for up to 4 joints.
        [FieldOffset(3)]
        public byte TSign;     // bit 0: tangent handedness, bit 1: tangent.z sign, bit 2: normal.z sign (0 means -1, 1 means +1).
        [FieldOffset(4)]
        public uint JointIndices0;
        [FieldOffset(8)]
        public uint JointIndices1;
        [FieldOffset(12)]
        public uint JointIndices2;
        [FieldOffset(16)]
        public uint JointIndices3;
        [FieldOffset(20)]
        public ushort NormalX;
        [FieldOffset(22)]
        public ushort NormalY;
        [FieldOffset(24)]
        public ushort TangentX;
        [FieldOffset(26)]
        public ushort TangentY;
        [FieldOffset(28)]
        public Vector2 Uv;
        [FieldOffset(32)]
        public byte Red;
        [FieldOffset(33)]
        public byte Green;
        [FieldOffset(34)]
        public byte Blue;
        [FieldOffset(35)]
        public byte Padding;

        public static int GetStride()
        {
            return Marshal.SizeOf(typeof(SkeletalNormalTextureColor));
        }
        public static void Write(MemoryStream ms, Vertex vertex)
        {
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.X));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Y));
            ms.WriteByte(PackingHelper.PackUnitFloat8(vertex.JointWeights.Z));
            ms.WriteByte(PackingHelper.PackTSign(vertex.Tangent));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[0]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[1]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[2]));
            ms.Write(BitConverter.GetBytes(vertex.JointIndices[3]));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Normal.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.X, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(PackingHelper.PackFloat16(vertex.Tangent.Y, -1f, 1f)));
            ms.Write(BitConverter.GetBytes(vertex.UV.X));
            ms.Write(BitConverter.GetBytes(vertex.UV.Y));
            ms.WriteByte(vertex.Red);
            ms.WriteByte(vertex.Green);
            ms.WriteByte(vertex.Blue);
            ms.WriteByte(0);
        }
    }
}
