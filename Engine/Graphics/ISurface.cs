
namespace Engine.Graphics
{
    public interface ISurface
    {
        int Width { get; }
        int Height { get; }

        void Resize(int width, int height);
        void Render();
    }
}
