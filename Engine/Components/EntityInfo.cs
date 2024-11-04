
namespace Engine.Components
{
    public struct EntityInfo()
    {
        public TransformInfo Transform { get; set; } = new();
        public ScriptInfo? Script { get; set; }
        public GeometryInfo? Geometry { get; set; }
    }
}
