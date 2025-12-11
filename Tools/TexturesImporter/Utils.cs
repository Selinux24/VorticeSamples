using System;

namespace TexturesImporter
{
    static class Utils
    {
        public static bool Equal(float a, float b, float epsilon = float.Epsilon)
        {
            return Math.Abs(a - b) <= epsilon;
        }
    }
}
