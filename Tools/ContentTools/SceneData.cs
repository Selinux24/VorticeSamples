
namespace ContentTools
{
    public class SceneData()
    {
        public byte[] Buffer { get; set; }
        public int BufferSize { get; set; }

        public GeometryImportSettings Settings = new();
    }
}
