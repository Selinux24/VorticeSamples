using PrimalLike.EngineAPI;

namespace PrimalLikeDLL
{
    public static class EntityAPI
    {
        public static Entity EntityFromId(uint id)
        {
            return new Entity(id);
        }
    }
}
