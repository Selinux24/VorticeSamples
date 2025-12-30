using System;
using System.Numerics;

namespace ContentTools
{
    public static class Utils
    {
        public const float Epsilon = 1e-5f;

        public static bool NearEqual(float n1, float n2, float eps = Epsilon)
        {
            return MathF.Abs(MathF.Abs(n1) - MathF.Abs(n2)) <= eps;
        }
        public static bool NearEqual(Vector2 a, Vector2 b, float eps = Epsilon)
        {
            return
                MathF.Abs(a.X - b.X) <= eps &&
                MathF.Abs(a.Y - b.Y) <= eps;
        }
        public static bool NearEqual(Vector3 a, Vector3 b, float eps = Epsilon)
        {
            return
                MathF.Abs(a.X - b.X) <= eps &&
                MathF.Abs(a.Y - b.Y) <= eps &&
                MathF.Abs(a.Z - b.Z) <= eps;
        }
        public static bool NearEqual(Vector4 a, Vector4 b, float eps = Epsilon)
        {
            return
                MathF.Abs(a.X - b.X) <= eps &&
                MathF.Abs(a.Y - b.Y) <= eps &&
                MathF.Abs(a.Z - b.Z) <= eps &&
                MathF.Abs(a.W - b.W) <= eps;
        }
        public static bool NotZero(float fX)
        {
            return MathF.Abs(fX) > float.MinValue;
        }
        public static bool NotZero(Vector3 v)
        {
            return NotZero(v.X) || NotZero(v.Y) || NotZero(v.Z);
        }
    }
}
