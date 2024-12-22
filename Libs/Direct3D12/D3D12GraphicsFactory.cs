using PrimalLike.Graphics;

namespace Direct3D12
{
    public class D3D12GraphicsFactory : IGraphicsPlatformFactory
    {
        /// <inheritdoc/>
        public IGraphicsPlatform CreateGraphicsPlatform()
        {
            return new D3D12Graphics();
        }
    }
}
