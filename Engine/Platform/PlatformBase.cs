using System.Drawing;

namespace Engine.Platform
{
    public abstract class PlatformBase
    {
        protected const string WINDOWTITLE = "Engine";
        protected readonly static Size WINDOWSIZE = new(800, 600);

        public abstract Window MainWindow { get; }

        protected PlatformBase()
        {

        }

        public abstract void Run();
    }
}
