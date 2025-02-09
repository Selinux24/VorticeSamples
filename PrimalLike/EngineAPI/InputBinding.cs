using System.Collections.Generic;

namespace PrimalLike.EngineAPI
{
    class InputBinding()
    {
        public List<InputSource> Sources { get; } = [];
        public InputValue Value { get; set; } = new();
        public bool IsDirty { get; set; } = true;
    }
}
