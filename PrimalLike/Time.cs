using System;
using System.Diagnostics;

namespace PrimalLike
{
    /// <summary>
    /// Time class to keep track of time in the game.
    /// </summary>
    public class Time
    {
        /// <summary>
        /// Stopwatch to keep track of time.
        /// </summary>
        private readonly Stopwatch stopwatch = new();

        /// <summary>
        /// Elapsed time since the last frame.
        /// </summary>
        public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;
        /// <summary>
        /// Total time since the game started.
        /// </summary>
        public TimeSpan Total { get; private set; } = TimeSpan.Zero;
        /// <summary>
        /// Delta time since the last frame.
        /// </summary>
        public float DeltaTime { get; private set; } = 0f;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Time()
        {
            stopwatch.Start();
        }

        /// <summary>
        /// Update the time.
        /// </summary>
        public void Update()
        {
            var elapsedTime = stopwatch.Elapsed - Total;

            Total = stopwatch.Elapsed;
            Elapsed = elapsedTime;

            DeltaTime = (float)Elapsed.TotalSeconds;
        }
        /// <summary>
        /// Stops the time.
        /// </summary>
        public void Stop()
        {
            stopwatch.Stop();
        }
    }
}
