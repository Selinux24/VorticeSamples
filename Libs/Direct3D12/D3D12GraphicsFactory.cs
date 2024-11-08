using Engine.Graphics;

namespace Direct3D12
{
    public class D3D12GraphicsFactory : IGraphicsFactory
    {
        /// <inheritdoc/>
        public GraphicsBase CreateGraphics()
        {
            return new D3D12Graphics();
        }
    }
}
