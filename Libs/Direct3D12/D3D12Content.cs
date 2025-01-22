using Direct3D12.Content;

namespace Direct3D12
{
    static class D3D12Content
    {
        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {
            // NOTE: we only release data that were created as a side-effect to adding resources,
            //       which the user of this module has no control over. The rest of data should be released
            //       by the user, by calling "remove" functions, prior to shutting down the renderer.
            //       That way we make sure the book-keeping of content is correct.

            Submesh.Shutdown();
            Texture.Shutdown();
            Material.Shutdown();
            RenderItem.Shutdown();
        }
    }
}
