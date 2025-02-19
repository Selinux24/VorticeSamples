using PrimalLike.EngineAPI;

namespace Direct3D12.Light
{
    struct LightOwner()
    {
        public uint EntityId = uint.MaxValue;
        public uint DataIndex = uint.MaxValue;
        public LightTypes LightType;
        public bool IsEnabled;
    }
}
