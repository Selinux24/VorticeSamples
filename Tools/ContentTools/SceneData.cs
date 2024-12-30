
namespace ContentTools
{
    public class SceneData(string name)
    {
        public string Name { get; set; } = name ?? "Imported Scene";
        public byte[] Buffer { get; set; }
        public int BufferSize { get; set; }
        public GeometryImportSettings Settings { get; set; } = new();
    }
}
