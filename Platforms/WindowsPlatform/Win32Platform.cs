using Engine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using static WindowsPlatform.Native.Ole32;
using static WindowsPlatform.Native.User32;

namespace WindowsPlatform
{
    class Win32Platform : Platform
    {
        const string WINDOWCLASSNAME = nameof(Win32Window);
        const uint WM_DESTROY = 2;
        const uint WM_PAINT = 0x0f;
        const uint WM_QUIT = 18U;

        delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static readonly Dictionary<nint, Win32Window> windows = [];

        private readonly IntPtr hInstance = Marshal.GetHINSTANCE(typeof(Win32Platform).Module);
        private readonly WndProcDelegate delegWndProc = WndProc;
        private readonly Win32Window mainWindow;

        public override Window MainWindow
        {
            get
            {
                return mainWindow;
            }
        }

        public Win32Platform() : base()
        {
            RegisterWindow();

            nint hwnd = CreateWindow(WINDOWTITLE, WINDOWSIZE);
            mainWindow = new Win32Window(hwnd, WINDOWTITLE);
            windows.Add(mainWindow.Handle, mainWindow);
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
                cbWndExtra = 0,
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
        public nint CreateWindow(string title, Size size)
        {
            RECT rect = new()
            {
                Right = size.Width,
                Bottom = size.Height
            };

            var style = WindowStyles.WS_OVERLAPPEDWINDOW;
            var styleEx = WindowStylesEx.WS_EX_APPWINDOW;

            AdjustWindowRectEx(
                ref rect,
                style,
                false,
                styleEx);

            nint hwnd = CreateWindowExW(
                styleEx,
                WINDOWCLASSNAME,
                title,
                style,
                0,
                0,
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

            return hwnd;
        }

        public override void Run()
        {
            mainWindow.Show();

            Vortice.Win32.NativeMessage msg = default;
            while (msg.msg != WM_QUIT)
            {
                if (PeekMessageW(out msg, IntPtr.Zero, 0, 0, (int)PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE) != 0)
                {
                    _ = TranslateMessage(ref msg);
                    _ = DispatchMessageW(ref msg);
                }
            }

            mainWindow.Destroy();

            CoUninitialize();
        }
        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (!windows.TryGetValue(hWnd, out _))
            {
                return DefWindowProcW(hWnd, msg, wParam, lParam);
            }

            switch (msg)
            {
                case WM_PAINT:
                    Application.Current.Tick();
                    break;

                case WM_DESTROY:
                    _ = PostQuitMessage(0);
                    break;

                default:
                    break;
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
    }
}
