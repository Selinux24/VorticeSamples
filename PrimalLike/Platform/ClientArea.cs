
namespace PrimalLike.Platform
{
    public struct ClientArea()
    {
        public uint Left { get; set; }
        public uint Top { get; set; }
        public readonly uint Right { get => Left + Width; }
        public readonly uint Bottom { get => Top + Height; }
        public uint Width { get; set; }
        public uint Height { get; set; }

        public ClientArea(uint left, uint top, uint width, uint height) : this()
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}
