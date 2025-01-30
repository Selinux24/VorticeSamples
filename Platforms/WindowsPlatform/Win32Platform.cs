using PrimalLike;
using PrimalLike.EngineAPI;
using PrimalLike.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Utilities;
using static WindowsPlatform.Native.Ole32;
using static WindowsPlatform.Native.User32;

namespace WindowsPlatform
{
    class Win32Platform : IPlatform
    {
        const string WINDOWCLASSNAME = nameof(Win32Window);
        const uint SIZE_MINIMIZED = 1;
        const uint WM_DESTROY = 0x0002;
        const uint WM_SIZE = 0x0005;
        const uint WM_CLOSE = 0x0010;
        const uint WM_QUIT = 0x0012;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_SYSCHAR = 0x0106;
        const uint WM_SYSCOMMAND = 0x0112;
        const uint WM_CAPTURECHANGED = 0x0215;
        const uint VK_RETURN = 0x0D;
        const uint VK_ESCAPE = 0x1B;
        const uint KF_ALTDOWN = 0x2000;
        const uint SC_KEYMENU = 0xF100;
        const WindowStyles FULL_SCREEN_STYLE = WindowStyles.WS_OVERLAPPED;
        const WindowStyles WINDOWED_STYLE = WindowStyles.WS_OVERLAPPEDWINDOW;

        private static bool resized = false;

        private static readonly FreeList<Win32Window> windows = new();
        private static readonly Dictionary<IntPtr, uint> windowsDict = [];

        private readonly WndProcDelegate delegWndProc = InternalWndProc;
        private readonly IntPtr hInstance = Marshal.GetHINSTANCE(typeof(Win32Platform).Module);
        private Win32Window mainWindow;

        public Win32Platform() : base()
        {
            RegisterWindow();
        }
        private void RegisterWindow()
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

            ushort atom = RegisterClassExW(ref wndClassEx);
            if (atom == 0)
            {
                string errorMessage = $"Failed to register window class. Error: {Marshal.GetLastWin32Error()}";

                throw new InvalidOperationException(errorMessage);
            }
        }

        public Window CreateWindow(IPlatformWindowInfo info, bool setDefault = true)
        {
            if (info is not Win32WindowInfo win32Info)
            {
                throw new ArgumentException("Invalid window info type.");
            }

            nint hwnd = CreateWindowInternal(win32Info);
            var wnd = new Win32Window(hwnd, info.ClientArea);

            if (mainWindow == null || setDefault)
            {
                mainWindow = wnd;
            }

            wnd.Id = windows.Add(wnd);
            windowsDict.Add(hwnd, wnd.Id);
            return new Window(wnd.Id);
        }
        private nint CreateWindowInternal(Win32WindowInfo info)
        {
            bool fullScreen = info.IsFullScreen;
            var caption = info.Caption;
            var clientArea = info.ClientArea;
            var style = fullScreen ? FULL_SCREEN_STYLE : WINDOWED_STYLE;

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

            nint hwnd = CreateWindowExW(
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

            if (hwnd == IntPtr.Zero)
            {
                string errorMessage = $"Failed to create window. Error: {Marshal.GetLastWin32Error()}";

                throw new InvalidOperationException(errorMessage);
            }

            if (info.WndProc != null)
            {
                IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(info.WndProc);
                SetWindowLongPtrW(hwnd, 0, callbackPtr);
            }

            Show(hwnd, fullScreen);

            return hwnd;
        }

        public void RemoveWindow(uint id)
        {
            var window = windows[id];
            windows.Remove(id);
            if (mainWindow.Id == id && windows.First(out var wnd))
            {
                mainWindow = wnd;
            }
            DestroyWindow(window.Handle);
            windowsDict.Remove(window.Handle);
        }


        public nint GetWindowHandle(uint id)
        {
            return windows[id].Handle;
        }
        public void SetFullscreen(uint id, bool isFullscreen)
        {
            windows[id].SetFullscreen(isFullscreen);
        }
        public bool IsWindoFullscreen(uint id)
        {
            return windows[id].IsFullscreen;
        }
        public void SetCaption(uint id, string caption)
        {
            windows[id].SetCaption(caption);
        }
        public void Resize(uint id, uint width, uint height)
        {
            windows[id].Resize(width, height);
        }
        public uint GetWindowWidth(uint id)
        {
            return windows[id].Width;
        }
        public uint GetWindowHeight(uint id)
        {
            return windows[id].Height;
        }
        public bool IsWindowClosed(uint id)
        {
            return windows[id].IsClosed;
        }


        public static void SetWindowTitle(IntPtr hwnd, string title)
        {
            SetWindowTextW(hwnd, title);
        }
        public static void SetWindowBounds(IntPtr hwnd, ClientArea bounds)
        {
            var windowRect = new RECT
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom
            };
            AdjustWindowRect(
                ref windowRect,
                WindowStyles.WS_VISIBLE,
                false);

            MoveWindow(
                hwnd,
                windowRect.Left,
                windowRect.Top,
                windowRect.GetWidth(),
                windowRect.GetHeight(),
                true);
        }
        public static void Show(IntPtr hwnd, bool maximize = false)
        {
            var nCmdShow = maximize ? ShowWindowCommands.SW_MAXIMIZE : ShowWindowCommands.SW_SHOWNORMAL;
            ShowWindow(hwnd, (int)nCmdShow);
        }
        public static void SetFullScreenStyle(IntPtr hwnd, bool fullScreen)
        {
            var style = fullScreen ? FULL_SCREEN_STYLE : WINDOWED_STYLE;

            IntPtr result = SetWindowLongPtrW(hwnd, (int)WindowLongIndex.GWL_STYLE, new IntPtr((uint)style));

            if (result == IntPtr.Zero)
            {
                string errorMessage = $"Failed to update the style. Error: {Marshal.GetLastWin32Error()}";

                throw new InvalidOperationException(errorMessage);
            }

            GetWindowRect(hwnd, out var rect);
            MoveWindow(
                hwnd,
                rect.Left,
                rect.Top,
                rect.GetWidth(),
                rect.GetHeight(),
                true);
        }

        public void Run()
        {
            NativeMessage msg = default;
            bool isRunning = true;
            while (isRunning)
            {
                Application.Current.Tick();

                while (PeekMessageW(out msg, IntPtr.Zero, 0, 0, (int)PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE) != 0)
                {
                    _ = TranslateMessage(ref msg);
                    _ = DispatchMessageW(ref msg);

                    if (msg.msg == WM_QUIT)
                    {
                        Debug.WriteLine("WM_QUIT received.");
                        isRunning = false;
                        Application.Current.Exit();
                        break;
                    }
                }
            }

            CoUninitialize();
        }

        private static IntPtr InternalWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (!windowsDict.TryGetValue(hwnd, out var id))
            {
                return DefWindowProcW(hwnd, msg, wParam, lParam);
            }

            var wnd = windows[id];

            bool toggleFullscreen = false;
            switch (msg)
            {
                case WM_DESTROY:
                    Debug.WriteLine($"Destroying window {hwnd}");
                    wnd.IsClosed = true;
                    if (!windowsDict.Any(w => !windows[w.Value].IsClosed))
                    {
                        return PostQuitMessage(0);
                    }
                    break;
                case WM_SIZE:
                    resized = wParam != SIZE_MINIMIZED;
                    break;
                case WM_SYSCHAR:
                    toggleFullscreen = wParam == VK_RETURN && (HiWord(lParam) & KF_ALTDOWN) != 0;
                    break;
                case WM_KEYDOWN:
                    if (wParam == VK_ESCAPE)
                    {
                        return PostMessage(hwnd, WM_CLOSE, 0, 0);
                    }
                    break;
                default:
                    break;
            }

            if (msg == WM_SYSCOMMAND && wParam == SC_KEYMENU)
            {
                return 0;
            }

            if (toggleFullscreen)
            {
                wnd.IsFullscreen = !wnd.IsFullscreen;

                return 0;
            }

            if (resized && msg == WM_CAPTURECHANGED)
            {
                Debug.WriteLine("Window Resized");
                SetResized(hwnd, out var clientArea);
                wnd.Resized(clientArea);
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
        private static IntPtr HiWord(IntPtr l)
        {
            return (ushort)((l >> 16) & 0xffff);
        }
        private static void SetResized(IntPtr hwnd, out ClientArea clientArea)
        {
            GetWindowRect(hwnd, out var rect);
            MoveWindow(
                hwnd,
                rect.Left,
                rect.Top,
                rect.GetWidth(),
                rect.GetHeight(),
                true);

            GetClientRect(hwnd, out var area);
            clientArea = new ClientArea(
                area.Left,
                area.Top,
                area.GetWidth(),
                area.GetHeight());
        }
    }
}
