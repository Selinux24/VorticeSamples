
namespace ContentTools
{
    /// <summary>
    /// Settings for importing geometry
    /// </summary>
    public class GeometryImportSettings()
    {
        public float SmoothingAngle { get; set; } = 0f;
        public bool CalculateNormals { get; set; } = false;
        public bool CalculateTangents { get; set; } = false;
        public bool ReverseHandedness { get; set; } = false;
        public bool ImportEmbeddedTextures { get; set; } = false;
        public bool ImportAnimations { get; set; } = false;
        public bool CoalesceMeshes { get; set; } = false;

        public bool IsLOD { get; set; } = false;
        public float[] Thresholds { get; set; } = [];
    }
}
