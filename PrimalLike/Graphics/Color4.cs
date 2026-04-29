using System;

namespace PrimalLike.Graphics
{
    public struct Color4 : IEquatable<Color4>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color4(byte value)
        {
            R = value;
            G = value;
            B = value;
            A = value;
        }
        public Color4(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public Color4(float r, float g, float b, float a)
        {
            // Clamp the input values to the range [0, 1] and convert to byte range [0, 255]
            R = (byte)(Math.Clamp(r, 0f, 1f) * 255f);
            G = (byte)(Math.Clamp(g, 0f, 1f) * 255f);
            B = (byte)(Math.Clamp(b, 0f, 1f) * 255f);
            A = (byte)(Math.Clamp(a, 0f, 1f) * 255f);
        }

        public static Color4 One { get; } = new Color4(255);
        public static Color4 Zero { get; } = new Color4(0);

        public readonly bool Equals(Color4 other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }
        public override readonly bool Equals(object obj)
        {
            return obj is Color4 color && Equals(color);
        }
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        public static bool operator ==(Color4 left, Color4 right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(Color4 left, Color4 right)
        {
            return !(left == right);
        }
    }
}
