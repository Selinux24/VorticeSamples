
namespace Engine.Platform
{
    public abstract class PlatformBase(PlatformWindowInfo info)
    {
        public PlatformWindowInfo MainWindowInfo { get; } = info;
        public abstract Window MainWindow { get; }

        public abstract void Run();
    }
}
