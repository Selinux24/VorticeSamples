using PrimalLike;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;
using System.Numerics;
using static Native32.User32;

namespace WindowsPlatform
{
    static class Win32Input
    {
        static ModifierFlags modifierKeysState = 0;

        public static void SetModifierInput(ushort virtualKey, InputCodes code, ModifierFlags flags)
        {
            if (GetKeyState(virtualKey) < 0)
            {
                Input.Set(InputSources.Keyboard, code, new(1f, 0f, 0f));
                modifierKeysState |= flags;
            }
            else if ((modifierKeysState & flags) != 0)
            {
                Input.Set(InputSources.Keyboard, code, new(0f, 0f, 0f));
                modifierKeysState &= ~flags;
            }
        }
        public static void SetModifierInputs(InputCodes code)
        {
            const ushort VK_LSHIFT = 0xA0;
            const ushort VK_RSHIFT = 0xA1;
            const ushort VK_LCONTROL = 0xA2;
            const ushort VK_RCONTROL = 0xA3;
            const ushort VK_LMENU = 0xA4;
            const ushort VK_RMENU = 0xA5;

            if (code == InputCodes.KeyShift)
            {
                SetModifierInput(VK_LSHIFT, InputCodes.KeyLeftShift, ModifierFlags.LeftShift);
                SetModifierInput(VK_RSHIFT, InputCodes.KeyRightShift, ModifierFlags.RightShift);
            }
            else if (code == InputCodes.KeyControl)
            {
                SetModifierInput(VK_LCONTROL, InputCodes.KeyLeftControl, ModifierFlags.LeftControl);
                SetModifierInput(VK_RCONTROL, InputCodes.KeyRightControl, ModifierFlags.RightControl);
            }
            else if (code == InputCodes.KeyAlt)
            {
                SetModifierInput(VK_LMENU, InputCodes.KeyLeftAlt, ModifierFlags.LeftAlt);
                SetModifierInput(VK_RMENU, InputCodes.KeyRightAlt, ModifierFlags.RightAlt);
            }
        }

        public static Vector2 GetMousePosition(IntPtr lParam)
        {
            return new(lParam & 0x0000ffff, lParam >> 16);
        }

        public static nint ProcessInputMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_KEYDOWN = 0x0100;
            const uint WM_KEYUP = 0x0101;
            const uint WM_SYSKEYDOWN = 0x0104;
            const uint WM_SYSKEYUP = 0x0105;
            const uint WM_MOUSEMOVE = 0x0200;
            const uint WM_LBUTTONDOWN = 0x0201;
            const uint WM_LBUTTONUP = 0x0202;
            const uint WM_RBUTTONDOWN = 0x0204;
            const uint WM_RBUTTONUP = 0x0205;
            const uint WM_MBUTTONDOWN = 0x0207;
            const uint WM_MBUTTONUP = 0x0208;
            const uint WM_MOUSEHWHEEL = 0x020E;

            switch (msg)
            {
                case WM_KEYDOWN:
                case WM_SYSKEYDOWN:
                {
                    Debug.Assert(wParam <= 0xff);
                    uint code = Keys.VkMapping[wParam & 0xff];
                    if (code != uint.MaxValue)
                    {
                        Input.Set(InputSources.Keyboard, (InputCodes)code, new(1f, 0f, 0f));
                        SetModifierInputs((InputCodes)code);
                    }
                }
                break;
                case WM_KEYUP:
                case WM_SYSKEYUP:
                {
                    Debug.Assert(wParam <= 0xff);
                    uint code = Keys.VkMapping[wParam & 0xff];
                    if (code != uint.MaxValue)
                    {
                        Input.Set(InputSources.Keyboard, (InputCodes)code, new(0f, 0f, 0f));
                        SetModifierInputs((InputCodes)code);
                    }
                }
                break;
                case WM_MOUSEMOVE:
                {
                    var pos = GetMousePosition(lParam);
                    Input.Set(InputSources.Mouse, InputCodes.MousePositionX, new(pos.X, 0f, 0f));
                    Input.Set(InputSources.Mouse, InputCodes.MousePositionY, new(pos.Y, 0f, 0f));
                    Input.Set(InputSources.Mouse, InputCodes.MousePosition, new(pos.X, pos.Y, 0f));
                }
                break;
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_MBUTTONDOWN:
                {
                    SetCapture(hwnd);
                    uint code = msg == WM_LBUTTONDOWN ?
                        (uint)InputCodes.MouseLeft : msg == WM_RBUTTONDOWN ?
                        (uint)InputCodes.MouseRight :
                        (uint)InputCodes.MouseMiddle;
                    var pos = GetMousePosition(lParam);
                    Input.Set(InputSources.Mouse, (InputCodes)code, new(pos.X, pos.Y, 1f));
                }
                break;
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                case WM_MBUTTONUP:
                {
                    ReleaseCapture();
                    uint code = msg == WM_LBUTTONUP ?
                        (uint)InputCodes.MouseLeft : msg == WM_RBUTTONUP ?
                        (uint)InputCodes.MouseRight :
                        (uint)InputCodes.MouseMiddle;
                    var pos = GetMousePosition(lParam);
                    Input.Set(InputSources.Mouse, (InputCodes)code, new(pos.X, pos.Y, 0f));
                }
                break;
                case WM_MOUSEHWHEEL:
                {
                    Input.Set(InputSources.Mouse, InputCodes.MouseWheel, new(GetWheelDeltaWParam(wParam), 0f, 0f));
                }
                break;
            }

            return 0;
        }
    }
}
