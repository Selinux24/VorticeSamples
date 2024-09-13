using System;
using System.Diagnostics;

namespace Engine
{
    public class Time
    {
        private readonly Stopwatch stopwatch = new();

        public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;
        public TimeSpan Total { get; private set; } = TimeSpan.Zero;


        public Time()
        {
            stopwatch.Start();
        }

        public void Update()
        {
            var elapsedTime = Elapsed - Total;

            Total = stopwatch.Elapsed;
            Elapsed = elapsedTime;
        }

        public void Stop()
        {
            stopwatch.Stop();
        }
    }
}
