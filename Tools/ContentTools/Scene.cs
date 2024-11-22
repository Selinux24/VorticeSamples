using System.Collections.Generic;

namespace ContentTools
{
    public class Scene
    {
        public string Name { get; set; }
        public List<LODGroup> LODGroups { get; set; } = [];
    }
}
