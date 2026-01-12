
namespace Direct3D12
{
    static class D3D12Upload
    {
        public static bool Initialize()
        {
            return UploadContext.Initialize();
        }
        public static void Shutdown()
        {
            UploadContext.Shutdown();
        }
    }
}
