
namespace PrimalLikeDLL
{
    public struct GameEntityDescriptor
    {
        public TransformComponent Transform { get; set; }
        public ScriptComponent Script { get; set; }
        public GeometryComponent Geometry { get; set; }
    }
}
