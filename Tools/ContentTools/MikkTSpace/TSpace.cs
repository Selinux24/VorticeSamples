using System.Numerics;

namespace ContentTools.MikkTSpace
{
    struct TSpace
    {
        public Vector3 Os;
        public Vector3 Ot;
        public float MagS;
        public float MagT;
        public int Counter;   // this is to average back into quads.
        public bool Orient;

        public readonly float Sign => Orient ? 1f : -1f;
    }
}
