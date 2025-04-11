
namespace TexturesImporter
{
    public class Slice
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint RowPitch { get; set; }
        public uint SlicePitch { get; set; }
        public byte[] RawContent { get; set; }
    }
}
