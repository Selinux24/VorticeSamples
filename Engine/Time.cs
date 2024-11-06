using System;
using System.Diagnostics;

namespace Engine
{
    public class Time
    {
        private readonly Stopwatch stopwatch = new();

        public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;
        public TimeSpan Total { get; private set; } = TimeSpan.Zero;
        public float DeltaTime { get; private set; } = 0f;

        public Time()
        {
            stopwatch.Start();
        }

        public void Update()
        {
            var elapsedTime = Total - Elapsed;

            Total = stopwatch.Elapsed;
            Elapsed = elapsedTime;

            DeltaTime = (float)Elapsed.TotalSeconds;
        }

        public void Stop()
        {
            stopwatch.Stop();
        }
    }
}
