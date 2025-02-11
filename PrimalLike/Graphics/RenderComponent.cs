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
        public RenderSurface Surface { get; private set; } = Application.CreateRenderSurface(info);

        /// <summary>
        /// Creates the camera.
        /// </summary>
        /// <param name="entity">Entity</param>
        public abstract void CreateCamera(Entity entity);

        /// <summary>
        /// Gets the frame info.
        /// </summary>
        /// <param name="time">Game time</param>
        public abstract FrameInfo GetFrameInfo(Time time);
        /// <summary>
        /// Renders the component.
        /// </summary>
        /// <param name="time">Game time</param>
        public virtual void Render(Time time)
        {
            if (!Surface.Surface.IsValid)
            {
                return;
            }

            var info = GetFrameInfo(time);
            Surface.Surface.Render(info);
        }
        /// <summary>
        /// Removes the component.
        /// </summary>
        public abstract void Remove();
    }
}
