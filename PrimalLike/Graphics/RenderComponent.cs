using PrimalLike.EngineAPI;
using PrimalLike.Platform;

namespace PrimalLike.Graphics
{
    /// <summary>
    /// Represents a render component.
    /// </summary>
    /// <param name="info">Window initialization info</param>
    public abstract class RenderComponent(IPlatformWindowInfo info)
    {
        /// <summary>
        /// Gets the window info.
        /// </summary>
        protected IPlatformWindowInfo Info { get; private set; } = info;
        /// <summary>
        /// Gets the render surface.
        /// </summary>
        public RenderSurface Surface { get; protected set; }

        /// <summary>
        /// Creates the camera.
        /// </summary>
        /// <param name="entity">Entity</param>
        public abstract void CreateCamera(Entity entity);

        /// <summary>
        /// Gets the frame info.
        /// </summary>
        public abstract FrameInfo GetFrameInfo();
        /// <summary>
        /// Renders the component.
        /// </summary>
        public void Render()
        {
            if (!Surface.Surface.IsValid)
            {
                return;
            }

            var info = GetFrameInfo();
            Surface.Surface.Render(info);
        }
        /// <summary>
        /// The component is resized.
        /// </summary>
        public abstract void Resized();
        /// <summary>
        /// Removes the component.
        /// </summary>
        public abstract void Remove();
    }
}
