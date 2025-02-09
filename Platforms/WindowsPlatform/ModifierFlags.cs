using System;

namespace WindowsPlatform
{
    [Flags]
    public enum ModifierFlags : uint
    {
        LeftShift = 0x10,
        LeftControl = 0x20,
        LeftAlt = 0x40,

        RightShift = 0x01,
        RightControl = 0x02,
        RightAlt = 0x04,
    }
}
