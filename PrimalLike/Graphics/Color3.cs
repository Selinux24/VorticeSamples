using System;
using System.Globalization;
using System.Text;

namespace PrimalLike.Graphics
{
    /// <summary>
    /// Color struct with 3 components (R, G, B) stored as bytes
    /// </summary>
    public struct Color3 : IEquatable<Color3>, IFormattable
    {
        /// <summary>
        /// Red
        /// </summary>
        public byte R;
        /// <summary>
        /// Green
        /// </summary>
        public byte G;
        /// <summary>
        /// Blue
        /// </summary>
        public byte B;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">The value to set for all components (R, G, B)</param>
        public Color3(byte value)
        {
            R = value;
            G = value;
            B = value;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        public Color3(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="r">Red component</param>
        /// <param name="g">Green component</param>
        /// <param name="b">Blue component</param>
        /// <remarks>The input values are clamped to the range [0, 1] and then converted to the byte range [0, 255]</remarks>
        public Color3(float r, float g, float b)
        {
            // Clamp the input values to the range [0, 1] and convert to byte range [0, 255]
            R = (byte)(Math.Clamp(r, 0f, 1f) * 255f);
            G = (byte)(Math.Clamp(g, 0f, 1f) * 255f);
            B = (byte)(Math.Clamp(b, 0f, 1f) * 255f);
        }

        /// <summary>
        /// Gets a color whose red, green, and blue components are all set to their maximum value.
        /// </summary>
        /// <remarks>This property represents the color white in the RGB color space, with each component set to 255. It can be used as a standard reference for full intensity in all channels.</remarks>
        public static Color3 One { get; } = new Color3(255);
        /// <summary>
        /// Gets a color whose red, green, and blue components are all set to their minimum value.
        /// </summary>
        /// <remarks>This property represents the color black in the RGB color space, with each component set to 0. It can be used as a standard reference for no intensity in all channels.</remarks>
        public static Color3 Zero { get; } = new Color3(0);

        /// <inheritdoc/>
        public readonly bool Equals(Color3 other)
        {
            return R == other.R && G == other.G && B == other.B;
        }
        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is Color3 color && Equals(color);
        }
        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        /// <inheritdoc/>
        public override readonly string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }
        /// <inheritdoc/>
        public readonly string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }
        /// <inheritdoc/>
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            sb.Append(R.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(G.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(B.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append('>');
            return sb.ToString();
        }

        /// <inheritdoc/>
        public static bool operator ==(Color3 left, Color3 right)
        {
            return left.Equals(right);
        }
        /// <inheritdoc/>
        public static bool operator !=(Color3 left, Color3 right)
        {
            return !(left == right);
        }
    }
}
