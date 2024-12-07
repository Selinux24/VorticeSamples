using Engine;
using Engine.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using static WindowsPlatform.Native.Ole32;
using static WindowsPlatform.Native.User32;

namespace WindowsPlatform
{
    class Win32Platform : PlatformBase
    {
        const string WINDOWCLASSNAME = nameof(Win32Window);
        const uint SIZE_MINIMIZED = 1;
        const uint WM_DESTROY = 0x0002;
        const uint WM_SIZE = 0x0005;
        const uint WM_CLOSE = 0x0010;
        const uint WM_QUIT = 0x0012;
        const uint WM_SYSCOMMAND = 0x0112;
        const uint SC_KEYMENU = 0xF100;
        const WindowStyles FULL_SCREEN_STYLE = WindowStyles.WS_OVERLAPPED;
        const WindowStyles WINDOWED_STYLE = WindowStyles.WS_OVERLAPPEDWINDOW;

        private static readonly Dictionary<IntPtr, Win32Window> windows = [];

        private readonly WndProcDelegate delegWndProc = InternalWndProc;
        private readonly IntPtr hInstance = Marshal.GetHINSTANCE(typeof(Win32Platform).Module);
        private Win32Window mainWindow;

        public override PlatformWindow MainWindow
        {
            get
            {
                return mainWindow;
            }
        }

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

        public override PlatformWindow CreateWindow(IPlatformWindowInfo info, bool setDefault = true)
        {
            if (info is not Win32WindowInfo win32Info)
            {
                throw new ArgumentException("Invalid window info type.");
            }

            nint hwnd = CreateWindowInternal(win32Info);
            var wnd = new Win32Window(hwnd);
            windows.Add(wnd.Handle, wnd);

            if (mainWindow == null || setDefault)
            {
                mainWindow = wnd;
            }

            return wnd;
        }
        private nint CreateWindowInternal(Win32WindowInfo info)
        {
            bool fullScreen = info.IsFullScreen;
            var title = info.Title;
            var clientArea = info.ClientArea;
            var style = fullScreen ? FULL_SCREEN_STYLE : WINDOWED_STYLE;

            RECT rect = new()
            {
                Left = clientArea.X,
                Top = clientArea.Y,
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
                title,
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

        public override void RemoveWindow(PlatformWindow window)
        {
            windows.Remove(window.Handle);
            if (mainWindow == window)
            {
                mainWindow = windows.Count != 0 ? windows.First().Value : null;
            }
            DestroyWindow(window.Handle);
        }

        public static void SetWindowTitle(IntPtr hwnd, string title)
        {
            SetWindowTextW(hwnd, title);
        }
        public static void SetWindowBounds(IntPtr hwnd, Rectangle bounds)
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

        public override void Run()
        {
            NativeMessage msg = default;
            bool isRunning = true;
            while (isRunning)
            {
                while (PeekMessageW(out msg, IntPtr.Zero, 0, 0, (int)PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE) != 0)
                {
                    _ = TranslateMessage(ref msg);
                    _ = DispatchMessageW(ref msg);

                    if (msg.msg == WM_QUIT)
                    {
                        Debug.WriteLine("WM_QUIT received.");
                        isRunning = false;
                        break;
                    }
                }

                Application.Current.Tick();
            }

            var windowsToRemove = windows.Values.ToArray();
            foreach (var wnd in windowsToRemove)
            {
                Application.Current.RemoveWindow(wnd);
            }
            Debug.Assert(windows.Count == 0);

            CoUninitialize();
        }
        private static IntPtr InternalWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (!windows.TryGetValue(hwnd, out var wnd))
            {
                return DefWindowProcW(hwnd, msg, wParam, lParam);
            }

            switch (msg)
            {
                case WM_DESTROY:
                    Debug.WriteLine($"Destroying window {hwnd}");
                    wnd.IsClosed = true;
                    if (!windows.Any(w => !w.Value.IsClosed))
                    {
                        return PostQuitMessage(0);
                    }
                    break;
                case WM_SIZE:
                    if (SetResized(hwnd, wParam != SIZE_MINIMIZED, out var clientArea))
                    {
                        Application.Current.ResizeWindow(wnd, clientArea);
                    }
                    break;
                default:
                    break;
            }

            if (msg == WM_SYSCOMMAND && wParam == SC_KEYMENU)
            {
                return 0;
            }

            var callbackPtr = GetWindowLongPtrW(hwnd, 0);
            if (callbackPtr == IntPtr.Zero)
            {
                return DefWindowProcW(hwnd, msg, wParam, lParam);
            }

            var callback = Marshal.GetDelegateForFunctionPointer<WndProcDelegate>(callbackPtr);
            return callback(hwnd, msg, wParam, lParam);
        }
        private static bool SetResized(IntPtr hwnd, bool resized, out Rectangle clientArea)
        {
            if (!resized)
            {
                clientArea = default;
                return false;
            }

            GetWindowRect(hwnd, out var rect);
            MoveWindow(
                hwnd,
                rect.Left,
                rect.Top,
                rect.GetWidth(),
                rect.GetHeight(),
                true);

            GetClientRect(hwnd, out var area);
            clientArea = new Rectangle(
                area.Left,
                area.Top,
                area.GetWidth(),
                area.GetHeight());

            return true;
        }
    }
}
