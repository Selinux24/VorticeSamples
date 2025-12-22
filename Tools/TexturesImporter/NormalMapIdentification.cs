using DirectXTexNet;
using System;
using System.Numerics;
using Utilities;

namespace TexturesImporter
{
    static class NormalMapIdentification
    {
        struct Color
        {
            public float R, G, B, A;
            public readonly bool IsTransparent() { return A < 0.001f; }
            public readonly bool IsBlack() { return R < 0.001f && G < 0.001f && B < 0.001f; }

            public static Color operator +(Color c1, Color c2)
            {
                return new Color
                {
                    R = c1.R + c2.R,
                    G = c1.G + c2.G,
                    B = c1.B + c2.B,
                    A = c1.A + c2.A
                };
            }
            public static Color operator *(Color c, float s)
            {
                return new Color
                {
                    R = c.R * s,
                    G = c.G * s,
                    B = c.B * s,
                    A = c.A * s
                };
            }
            public static Color operator /(Color c, float s)
            {
                return c * (1f / s);
            }
        }

        private const float Inv255 = 1f / 255f;
        private const float MinAvgLengthThreshold = 0.7f;
        private const float MaxAvgLengthThreshold = 1.1f;
        private const float MinAvgZThreshold = 0.8f;
        private const float VectorLengthSqRejectionThreshold = MinAvgLengthThreshold * MinAvgLengthThreshold;
        private const float RejectionRatioThreshold = 0.33f;

        delegate Color Sampler(IntPtr ptr);

        public static bool IsNormalMap(Image image)
        {
            TexHelper texHelper = TexHelper.Instance;
            DXGI_FORMAT imageFormat = image.Format;

            if (texHelper.BitsPerPixel(imageFormat) != 32 || texHelper.BitsPerColor(imageFormat) != 8) return false;

            return EvaluateImage(image, texHelper.IsBGR(imageFormat) ? SamplePixelBgr : SamplePixelRgb);
        }
        private static bool EvaluateImage(Image image, Sampler sample)
        {
            uint sampleCount = 4096;
            long imageSize = image.SlicePitch;
            int sampleInterval = (int)Math.Max(imageSize / sampleCount, 4L);
            uint minSampleCount = Math.Max((uint)(imageSize / sampleInterval) >> 2, 1U);
            IntPtr pixels = image.Pixels;

            uint acceptedSamples = 0;
            uint rejectedSamples = 0;
            Color averageColor = new();

            int offset = sampleInterval;
            while (offset < imageSize)
            {
                Color c = sample(pixels + offset);
                int result = EvaluateColor(c);
                if (result < 0)
                {
                    rejectedSamples++;
                }
                else if (result > 0)
                {
                    acceptedSamples++;
                    averageColor += c;
                }

                offset += sampleInterval;
            }

            if (acceptedSamples >= minSampleCount)
            {
                float rejectionRatio = (float)rejectedSamples / acceptedSamples;
                if (rejectionRatio > RejectionRatioThreshold)
                {
                    return false;
                }

                averageColor /= acceptedSamples;
                Vector3 v = new(averageColor.R * 2f - 1f, averageColor.G * 2f - 1f, averageColor.B * 2f - 1f);
                float avgLength = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
                float avgNormalizedZ = v.Z / avgLength;

                return
                    avgLength >= MinAvgLengthThreshold &&
                    avgLength <= MaxAvgLengthThreshold &&
                    avgNormalizedZ >= MinAvgZThreshold;
            }

            return false;
        }
        private static int EvaluateColor(Color c)
        {
            if (c.IsBlack() || c.IsTransparent())
            {
                return 0;
            }

            Vector3 v = new(c.R * 2f - 1f, c.G * 2f - 1f, c.B * 2f - 1f);
            float vLengthSq = v.X * v.X + v.Y * v.Y + v.Z * v.Z;

            return (v.Z < 0f || vLengthSq < VectorLengthSqRejectionThreshold) ? -1 : 1;
        }
        private static Color SamplePixelBgr(IntPtr ptr)
        {
            BlobStreamReader reader = new(ptr);

            Color c = new()
            {
                B = reader.Read<byte>(),
                G = reader.Read<byte>(),
                R = reader.Read<byte>(),
                A = reader.Read<byte>(),
            };

            return c * Inv255;
        }
        private static Color SamplePixelRgb(IntPtr ptr)
        {
            BlobStreamReader reader = new(ptr);

            Color c = new()
            {
                R = reader.Read<byte>(),
                G = reader.Read<byte>(),
                B = reader.Read<byte>(),
                A = reader.Read<byte>(),
            };

            return c * Inv255;
        }
    }
}
