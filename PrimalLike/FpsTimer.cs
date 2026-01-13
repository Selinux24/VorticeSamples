using System;
using System.Diagnostics;

namespace PrimalLike
{
    /// <summary>
    /// FPS timer.
    /// </summary>
    static class FpsTimer
    {
        static readonly Stopwatch stopwatch = new();
        static float msAvg = 0f;
        static int counter = 1;
        static DateTime seconds = DateTime.Now;

        /// <summary>
        /// Gets the frames per second.
        /// </summary>
        public static float FramesPerSecond { get; private set; } = 0f;

        /// <summary>
        /// Begins the timer.
        /// </summary>
        public static void Begin()
        {
            stopwatch.Restart();
        }
        /// <summary>
        /// Ends the timer.
        /// </summary>
        public static void End()
        {
            stopwatch.Stop();
            float dt = stopwatch.ElapsedMilliseconds;
            msAvg += (dt - msAvg) / counter;
            counter++;

            if ((DateTime.Now - seconds).TotalSeconds >= 1)
            {
                Trace.WriteLine($"Avg. frame (ms): {msAvg:0.0000} {counter:0000} fps");
                FramesPerSecond = msAvg;
                msAvg = 0f;
                counter = 1;
                seconds = DateTime.Now;
            }
        }
    }
}
