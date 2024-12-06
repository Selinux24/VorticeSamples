using System;

namespace Engine.Graphics
{
    /// <summary>
    /// Surface interface.
    /// </summary>
    public interface ISurface : IDisposable
    {
        /// <summary>
        /// Width
        /// </summary>
        int Width { get; }
        /// <summary>
        /// Height
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Resizes the surface.
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        void Resize(int width, int height);
    }
}
