using System.Numerics;

namespace ContentTools.MikkTSpace
{
    struct TmpVert
    {
        public Vector3 Vert;
        public int Index;

        public readonly override string ToString()
        {
            return $"{Vert} {Index}";
        }
    }
}
