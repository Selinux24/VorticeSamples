using System.Collections.Generic;

namespace ContentTools
{
    public class LODGroup()
    {
        public string Name { get; set; }
        public List<MeshLOD> LODs { get; set; } = [];
    }
}
