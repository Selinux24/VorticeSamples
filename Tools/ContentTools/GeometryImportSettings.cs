
namespace ContentTools
{
    /// <summary>
    /// Settings for importing geometry
    /// </summary>
    public class GeometryImportSettings()
    {
        public bool CalculateNormals { get; set; } = false;
        public bool CalculateTangents { get; set; } = true;
        public float SmoothingAngle { get; set; } = 178f;
        public bool ReverseHandedness { get; set; } = false;
        public bool ImportEmbeddedTextures { get; set; } = true;
        public bool ImportAnimations { get; set; } = true;
        public bool CoalesceMeshes { get; set; } = false;

        public bool IsLOD { get; set; } = false;
        public float[] Thresholds { get; set; } = [];
    }
}
