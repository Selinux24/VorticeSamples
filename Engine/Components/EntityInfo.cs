
namespace Engine.Components
{
    public struct EntityInfo()
    {
        public TransformInfo TransformInfo { get; set; } = new();
        public ScriptInfo? ScriptInfo { get; set; }
        public GeometryInfo? GeometryInfo { get; set; }
    }
}
