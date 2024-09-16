using Engine.Graphics;

namespace Direct3D12
{
    public class D3D12Surface : ISurface
    {
        /// <inheritdoc/>
        public uint Width { get; private set; }
        /// <inheritdoc/>
        public uint Height { get; private set; }
    }
}
