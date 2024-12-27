using System;
using System.Diagnostics;

namespace PrimalLike
{
    static class FpsTimer
    {
        private static readonly Stopwatch stopwatch = new();
        private static float msAvg = 0f;
        private static int counter = 1;
        private static DateTime seconds = DateTime.Now;

        public static void Begin()
        {
            stopwatch.Restart();
        }

        public static void End()
        {
            stopwatch.Stop();
            float dt = stopwatch.ElapsedMilliseconds;
            msAvg += (dt - msAvg) / counter;
            counter++;

            if ((DateTime.Now - seconds).TotalSeconds >= 1)
            {
                Debug.WriteLine($"Avg. frame (ms): {msAvg} {counter} fps");
                msAvg = 0f;
                counter = 1;
                seconds = DateTime.Now;
            }
        }
    }
}
