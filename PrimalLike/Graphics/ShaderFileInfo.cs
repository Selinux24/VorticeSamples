using System;

namespace PrimalLike.Graphics
{
    public readonly struct ShaderFileInfo(string fileName, string entryPoint, ShaderTypes stage, string profile = null)
    {
        public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));
        public string EntryPoint { get; } = entryPoint ?? throw new ArgumentNullException(nameof(entryPoint));
        public ShaderTypes Stage { get; } = stage;
        public string Profile { get; } = profile;
    }
}
