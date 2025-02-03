using PrimalLike.EngineAPI;

namespace Direct3D12.Lights
{
    record LightOwner
    {
        public uint EntityId;
        public uint DataIndex;
        public LightTypes LightType;
        public bool IsEnabled;
    }
}
