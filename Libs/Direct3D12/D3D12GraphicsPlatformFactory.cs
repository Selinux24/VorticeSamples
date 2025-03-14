﻿using PrimalLike.Graphics;

namespace Direct3D12
{
    /// <summary>
    /// Graphics platform factory for Direct3D12.
    /// </summary>
    public class D3D12GraphicsPlatformFactory : IGraphicsPlatformFactory
    {
        /// <inheritdoc/>
        public IGraphicsPlatform CreateGraphicsPlatform()
        {
            return new D3D12GraphicsPlatform();
        }
    }
}
