
namespace ContentTools
{
    public class GeometryImportSettings
    {
        public float SmoothingAngle { get; set; }
        public bool CalculateNormals { get; set; }
        public bool CalculateTangents { get; set; }
        public bool ReverseHandedness { get; set; }
        public bool ImportEmbeddedTextures { get; set; }
        public bool ImportAnimations { get; set; }
        public bool CoalesceMeshes { get; set; }
    }
}
