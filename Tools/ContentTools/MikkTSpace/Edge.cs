using System;

namespace ContentTools.MikkTSpace
{
    struct Edge
    {
        public int I0;
        public int I1;
        public int F;

        public int this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => I0,
                    1 => I1,
                    2 => F,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        I0 = value;
                        break;
                    case 1:
                        I1 = value;
                        break;
                    case 2:
                        F = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public readonly override string ToString()
        {
            return $"I0={I0}; I1={I1}; F={F};";
        }
    }
}
