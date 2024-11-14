
namespace Engine.Platform
{
    public interface IPlatformFactory
    {
        PlatformBase CreatePlatform(PlatformWindowInfo info);
    }
}
