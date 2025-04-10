﻿
namespace PrimalLike.Platform
{
    /// <summary>
    /// Platform factory interface.
    /// </summary>
    public interface IPlatformFactory
    {
        /// <summary>
        /// Create a platform.
        /// </summary>
        /// <returns>Returns the create platform</returns>
        IPlatform CreatePlatform();
    }
}
