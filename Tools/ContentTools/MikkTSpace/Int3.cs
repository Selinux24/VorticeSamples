using System;

namespace ContentTools.MikkTSpace
{
    struct Int3
    {
        public int I0;
        public int I1;
        public int I2;

        public int this[int index]
        {
            readonly get
            {
                return index switch
                {
                    0 => I0,
                    1 => I1,
                    2 => I2,
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
                        I2 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
