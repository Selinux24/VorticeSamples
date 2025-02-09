using System;

namespace PrimalLike.EngineAPI
{
    public struct InputSource()
    {
        public ulong Binding { get; }
        public InputSources SourceType { get; set; }
        public uint Code { get; set; } = 0;
        public float Multiplier { get; set; } = 0f;
        public bool IsDiscrete { get; set; } = true;
        public InputAxis SourceAxis { get; set; }
        public InputAxis Axis { get; set; }
        public ModifierKeys Modifier { get; set; }

        public InputSource(string bindingName) : this()
        {
            ArgumentNullException.ThrowIfNull(bindingName);

            Binding = (ulong)bindingName.GetHashCode();
        }

        public override readonly string ToString()
        {
            return $"{(InputCodes)Code}·{Multiplier}->{Binding}";
        }
    }
}
