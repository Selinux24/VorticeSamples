using System;

namespace PrimalLike.Graphics
{
    public struct Color3 : IEquatable<Color3>
    {
        public byte R;
        public byte G;
        public byte B;

        public Color3(byte value)
        {
            R = value;
            G = value;
            B = value;
        }
        public Color3(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        public Color3(float r, float g, float b)
        {
            // Clamp the input values to the range [0, 1] and convert to byte range [0, 255]
            R = (byte)(Math.Clamp(r, 0f, 1f) * 255f);
            G = (byte)(Math.Clamp(g, 0f, 1f) * 255f);
            B = (byte)(Math.Clamp(b, 0f, 1f) * 255f);
        }

        public static Color3 One { get; } = new Color3(255);
        public static Color3 Zero { get; } = new Color3(0);

        public readonly bool Equals(Color3 other)
        {
            return R == other.R && G == other.G && B == other.B;
        }
        public override readonly bool Equals(object obj)
        {
            return obj is Color3 color && Equals(color);
        }
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        public static bool operator ==(Color3 left, Color3 right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(Color3 left, Color3 right)
        {
            return !(left == right);
        }
    }
}
