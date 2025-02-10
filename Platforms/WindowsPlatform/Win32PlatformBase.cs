using PrimalLike.EngineAPI;
using PrimalLike.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Utilities;
using static Native32.Ole32;
using static Native32.User32;
using static Native32.Kernel32;

namespace WindowsPlatform
{
    static class Win32PlatformBase
    {
        const string WINDOWCLASSNAME = nameof(Win32PlatformBase);
        const WindowStyles FULL_SCREEN_STYLE = WindowStyles.WS_OVERLAPPED;
        const WindowStyles WINDOWED_STYLE = WindowStyles.WS_OVERLAPPEDWINDOW;

        private static readonly FreeList<WindowInfo> windows = new();
        private static bool resized = false;
        private static readonly IntPtr hInstance = Marshal.GetHINSTANCE(typeof(Win32PlatformBase).Module);
        private static readonly WndProcDelegate delegWndProc = InternalWndProc;

        private static WindowInfo GetFromId(uint id)
        {
            Debug.Assert(windows[id].Hwnd != IntPtr.Zero);
            return windows[id];
        }
        private static WindowInfo GetFromHandle(IntPtr handle)
        {
            uint id = (uint)GetWindowLongPtrW(handle, (int)WindowLongIndex.GWL_USERDATA);
            return GetFromId(id);
        }

        private static IntPtr InternalWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_NCCREATE:
                {
                    // Put the window id in the user data field of window's data buffer.
                    SetLastError(0);
                    uint id = windows.Add(new());
                    windows[id].Hwnd = hwnd;
                    SetWindowLongPtrW(hwnd, (int)WindowLongIndex.GWL_USERDATA, (IntPtr)id);
                    Debug.Assert(GetLastError() == 0);
                }
                break;
                case WM_DESTROY:
                    WindowInfo info = GetFromHandle(hwnd);
                    info.IsClosed = true;
                    break;
                case WM_SIZE:
                    resized = wParam != SIZE_MINIMIZED;
                    break;

                default:
                    break;
            }

            Win32Input.ProcessInputMessage(hwnd, msg, wParam, lParam);

            if (resized && GetKeyState((int)VK_LBUTTON) >= 0)
            {
                WindowInfo info = GetFromHandle(hwnd);
                Debug.Assert(info.Hwnd != IntPtr.Zero);
                if (info.IsFullScreen)
                {
                    GetClientRect(info.Hwnd, out info.FullScreenArea);
                }
                else
                {
                    GetClientRect(info.Hwnd, out info.ClientArea);
                }
                resized = false;
            }

            var callbackPtr = GetWindowLongPtrW(hwnd, 0);
            if (callbackPtr != IntPtr.Zero)
            {
                var callback = Marshal.GetDelegateForFunctionPointer<WndProcDelegate>(callbackPtr);
                return callback(hwnd, msg, wParam, lParam);
            }
            return DefWindowProcW(hwnd, msg, wParam, lParam);
        }
      
        public static void ResizeWindow(WindowInfo info, ref RECT area)
        {
            // Adjust the window size for correct device size
            RECT windowRect = area;
            AdjustWindowRect(ref windowRect, info.Style, false);

            uint width = windowRect.Right - windowRect.Left;
            uint height = windowRect.Bottom - windowRect.Top;

            MoveWindow(info.Hwnd, (uint)info.TopLeft.X, (uint)info.TopLeft.Y, width, height, true);
        }
        public static void ResizeWindow(uint id, uint width, uint height)
        {
            WindowInfo info = GetFromId(id);

            // NOTE: when we host the window in the level editor we just update
            //       the internal data (i.e. the client area dimensions).
            if ((info.Style & WindowStyles.WS_CHILD) != 0)
            {
                GetClientRect(info.Hwnd, out info.ClientArea);
            }
            else
            {
                // NOTE: we also resize while in fullscreen mode to support the case
                //       when the user changes the screen resolution.
                if (info.IsFullScreen)
                {
                    info.FullScreenArea.Bottom = info.FullScreenArea.Top + height;
                    info.FullScreenArea.Right = info.FullScreenArea.Left + width;

                    ResizeWindow(info, ref info.FullScreenArea);
                }
                else
                {
                    info.ClientArea.Bottom = info.ClientArea.Top + height;
                    info.ClientArea.Right = info.ClientArea.Left + width;

                    ResizeWindow(info, ref info.ClientArea);
                }
            }
        }

        public static void SetWindowFullscreen(uint id, bool isFullscreen)
        {
            WindowInfo info = GetFromId(id);

            if (info.IsFullScreen != isFullscreen)
            {
                info.IsFullScreen = isFullscreen;

                if (isFullscreen)
                {
                    // Store the current window dimensions so they can be restored
                    // when switching out of fullscreen state.
                    GetClientRect(info.Hwnd, out info.ClientArea);
                    GetWindowRect(info.Hwnd, out var rect);
                    info.TopLeft.X = (int)rect.Left;
                    info.TopLeft.Y = (int)rect.Top;
                    SetWindowLongPtrW(info.Hwnd, (int)WindowLongIndex.GWL_STYLE, 0);
                    ShowWindow(info.Hwnd, (int)ShowWindowCommands.SW_MAXIMIZE);
                }
                else
                {
                    SetWindowLongPtrW(info.Hwnd, (int)WindowLongIndex.GWL_STYLE, (IntPtr)info.Style);
                    ResizeWindow(info, ref info.ClientArea);
                    ShowWindow(info.Hwnd, (int)ShowWindowCommands.SW_SHOWNORMAL);
                }
            }
        }
        public static bool IsWindowFullscreen(uint id)
        {
            return GetFromId(id).IsFullScreen;
        }
        public static IntPtr GetWindowHandle(uint id)
        {
            return GetFromId(id).Hwnd;
        }
        public static void SetWindowCaption(uint id, string caption)
        {
            WindowInfo info = GetFromId(id);
            SetWindowTextW(info.Hwnd, caption);
        }
        public static ClientArea GetWindowSize(uint id)
        {
            WindowInfo info = GetFromId(id);
            RECT area = info.IsFullScreen ? info.FullScreenArea : info.ClientArea;
            return new(area.Left, area.Top, area.Right, area.Bottom);
        }
        public static bool IsWindowClosed(uint id)
        {
            return GetFromId(id).IsClosed;
        }

        public static Window CreateWindow(Win32WindowInfo initInfo)
        {
            RegisterWindow();

            bool fullScreen = initInfo.IsFullScreen;
            var caption = initInfo.Caption ?? nameof(PrimalLike);
            var clientArea = initInfo.ClientArea;
            var style = fullScreen ? FULL_SCREEN_STYLE : WINDOWED_STYLE;
            var parent = initInfo.Parent;

            WindowInfo info = new();
            info.ClientArea.Right = initInfo.ClientArea.Width != 0 ? info.ClientArea.Left + initInfo.ClientArea.Width : info.ClientArea.Right;
            info.ClientArea.Bottom = initInfo.ClientArea.Height != 0 ? info.ClientArea.Top + initInfo.ClientArea.Height : info.ClientArea.Bottom;
            info.Style |= parent != IntPtr.Zero ? WindowStyles.WS_CHILD : WindowStyles.WS_OVERLAPPEDWINDOW;

            RECT rect = new()
            {
                Left = clientArea.Left,
                Top = clientArea.Top,
                Right = clientArea.Right,
                Bottom = clientArea.Bottom,
            };

            AdjustWindowRect(
                ref rect,
                style,
                false);

            info.Hwnd = CreateWindowExW(
                WindowStylesEx.WS_EX_LEFT,
                WINDOWCLASSNAME,
                caption,
                style,
                clientArea.Left,
                clientArea.Top,
                rect.GetWidth(),
                rect.GetHeight(),
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);

            if (info.Hwnd == IntPtr.Zero)
            {
                string errorMessage = $"Failed to create window. Error: {Marshal.GetLastWin32Error()}";

                throw new InvalidOperationException(errorMessage);
            }

            if (initInfo.Callback != null)
            {
                IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(initInfo.Callback);
                SetWindowLongPtrW(info.Hwnd, 0, callbackPtr);
            }

            ShowWindow(info.Hwnd, (int)ShowWindowCommands.SW_SHOWNORMAL);
            UpdateWindow(info.Hwnd);

            uint id = (uint)GetWindowLongPtrW(info.Hwnd, (int)WindowLongIndex.GWL_USERDATA);
            windows[id] = info;

            return new Window(id);
        }
        private static void RegisterWindow()
        {
            CoInitializeEx(IntPtr.Zero, COINIT.COINIT_APARTMENTTHREADED);

            WNDCLASSEXW wndClassEx = new()
            {
                cbSize = Marshal.SizeOf<WNDCLASSEXW>(),
                style = ClassStyles.HorizontalRedraw | ClassStyles.VerticalRedraw | ClassStyles.OwnDC,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc),
                cbClsExtra = 0,
                cbWndExtra = Marshal.SizeOf<IntPtr>(),
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = LoadCursorW(IntPtr.Zero, (int)IDC_STANDARD_CURSORS.IDC_ARROW),
                hbrBackground = default,
                lpszMenuName = null,
                lpszClassName = WINDOWCLASSNAME,
                hIconSm = IntPtr.Zero,
            };

            _ = RegisterClassExW(ref wndClassEx);
        }
        public static void UnregisterWindow()
        {
            CoUninitialize();
        }

        public static void RemoveWindow(uint id)
        {
            WindowInfo info = GetFromId(id);
            DestroyWindow(info.Hwnd);
            windows.Remove(id);
        }
    }
}
