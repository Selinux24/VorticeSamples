global using SurfaceId = System.UInt32;
using PrimalLike.Common;
using System.Diagnostics;

namespace PrimalLike.Graphics
{
    public class Surface
    {
        private readonly SurfaceId id = SurfaceId.MaxValue;

        public Surface()
        {

        }
        public Surface(SurfaceId id)
        {
            this.id = id;
        }

        public SurfaceId Id { get => id; }
        public bool IsValid { get => IdDetail.IsValid(id); }

        /// <summary>
        /// Width
        /// </summary>
        public uint Width
        {
            get
            {
                Debug.Assert(IsValid);
                return Renderer.Gfx.GetSurfaceWidth(id);
            }
        }
        /// <summary>
        /// Height
        /// </summary>
        public uint Height
        {
            get
            {
                Debug.Assert(IsValid);
                return Renderer.Gfx.GetSurfaceHeight(id);
            }
        }

        /// <summary>
        /// Resizes the surface.
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        public void Resize(uint width, uint height)
        {
            Debug.Assert(IsValid);
            Renderer.Gfx.ResizeSurface(id, width, height);
        }
        /// <summary>
        /// Renders on the surface.
        /// </summary>
        /// <param name="info">Frame info</param>
        public void Render(FrameInfo info)
        {
            Debug.Assert(IsValid);
            Renderer.Gfx.RenderSurface(id, info);
        }
    }
}
