using System.Collections.Generic;

namespace ContentTools
{
    public class Scene(string name)
    {
        public string Name { get; set; } = name;
        public List<LODGroup> LODGroups { get; set; } = [];
    }
}
