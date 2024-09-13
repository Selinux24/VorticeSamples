using System.Drawing;

namespace Engine
{
    public abstract class Platform
    {
        protected const string WINDOWTITLE = "Engine";
        protected readonly static Size WINDOWSIZE = new(800, 600);

        public abstract Window MainWindow { get; }

        protected Platform()
        {

        }

        public abstract void Run();
    }
}
