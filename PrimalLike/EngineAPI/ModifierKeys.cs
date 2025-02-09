
namespace PrimalLike.EngineAPI
{
    public enum ModifierKeys : uint
    {
        None = 0x00,
        LeftShift = 0x01,
        RightShift = 0x02,
        Shift = LeftShift | RightShift,
        LeftCtrl = 0x04,
        RightCtrl = 0x08,
        Ctrl = LeftCtrl | RightCtrl,
        LeftAlt = 0x10,
        RightAlt = 0x20,
        Alt = LeftAlt | RightAlt,
    }
}
