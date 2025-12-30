using System.Numerics;

namespace ContentTools.MikkTSpace
{
    struct TriInfo()
    {
        public readonly int[] FaceNeighbors = new int[3];
        public readonly Group[] AssignedGroup = new Group[3];

        // normalized first order face derivatives
        public Vector3 Os;
        public Vector3 Ot;
        public float MagS;
        public float MagT; // original magnitudes

        // determines if the current and the next triangle are a quad.
        public int OrgFaceNumber;
        public int Flag = 0;
        public int TSpacesOffs;
        public readonly int[] VertNum = new int[4];

        public readonly override string ToString()
        {
            return $"FaceNeighbors={{{FaceNeighbors[0]},{FaceNeighbors[1]},{FaceNeighbors[2]}}}; Os={Os}; Ot={Ot}; MagS={MagS}; MagT={MagT};";
        }
    }
}
