using System;

namespace Direct3D12.ShaderCompiler
{
    public readonly struct ShaderFileInfo(string fileName, string entryPoint, uint stage, string profile = null)
    {
        public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));
        public string EntryPoint { get; } = entryPoint ?? throw new ArgumentNullException(nameof(entryPoint));
        public uint Stage { get; } = stage;
        public string Profile { get; } = profile ?? Compiler.GetShaderProfile(stage);
    }
}
