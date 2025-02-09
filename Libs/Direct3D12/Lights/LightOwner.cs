using PrimalLike.EngineAPI;

namespace Direct3D12.Lights
{
    record LightOwner()
    {
        public uint EntityId = uint.MaxValue;
        public uint DataIndex = uint.MaxValue;
        public LightTypes LightType;
        public bool IsEnabled;
    }
}
