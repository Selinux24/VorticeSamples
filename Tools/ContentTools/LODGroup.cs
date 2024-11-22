using System.Collections.Generic;

namespace ContentTools
{
    public class LODGroup()
    {
        public string Name { get; set; }
        public List<Mesh> Meshes { get; set; } = [];
    }
}
