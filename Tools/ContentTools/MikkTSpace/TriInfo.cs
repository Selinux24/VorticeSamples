using System.Numerics;

namespace ContentTools.MikkTSpace
{
    struct TriInfo()
    {
        public int[] FaceNeighbors = new int[3];
        public Group[] AssignedGroup = new Group[3];

        // normalized first order face derivatives
        public Vector3 Os;
        public Vector3 Ot;
        public float MagS;
        public float MagT; // original magnitudes

        // determines if the current and the next triangle are a quad.
        public int OrgFaceNumber;
        public int Flag;
        public int TSpacesOffs;
        public int[] VertNum = new int[4];
    }
}
