using System.Collections.Generic;

namespace ContentTools
{
    public class MeshLOD
    {
        public string Name { get; set; }
        public float Threshold { get; set; }
        public List<Mesh> Meshes { get; set; } = [];
    }
}
