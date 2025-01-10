using System;
using Vortice.Dxc;

namespace ShaderCompiler
{
    public readonly struct ShaderFileInfo(string fileName, string entryPoint, ShaderStage stage, string profile = null)
    {
        public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));
        public string EntryPoint { get; } = entryPoint ?? throw new ArgumentNullException(nameof(entryPoint));
        public ShaderStage Stage { get; } = stage;
        public string Profile { get; } = profile ?? DxcCompiler.GetShaderProfile((DxcShaderStage)stage, ShaderCompiler.DefaultShaderModel);
    }
}
