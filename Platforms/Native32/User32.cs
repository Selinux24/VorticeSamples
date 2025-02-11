using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("WindowsPlatform")]
[assembly: InternalsVisibleTo("DX12Windows")]
[assembly: InternalsVisibleTo("D3D12LibTests")]
namespace Native32
{
    static partial class User32
    {
        const string LibraryName = "user32.dll";

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AdjustWindowRect(
            ref RECT lpRect,
            uint dwStyle,
            [MarshalAs(UnmanagedType.Bool)] bool bMenu);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool AdjustWindowRectEx(
            ref RECT lpRect,
            uint dwStyle,
            [MarshalAs(UnmanagedType.Bool)] bool bMenu,
            uint dwExStyle);

        [LibraryImport(LibraryName, SetLastError = true, EntryPoint = "RegisterClassExW")]
        [return: MarshalAs(UnmanagedType.U2)]
        public static partial ushort RegisterClassExW(ref WNDCLASSEXW lpWndClass);

        [LibraryImport(LibraryName, SetLastError = true)]
        public static partial IntPtr CreateWindowExW(
            uint dwExStyle,
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle,
            uint x,
            uint y,
            uint nWidth,
            uint nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [LibraryImport(LibraryName)]
        public static partial IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [LibraryImport(LibraryName, SetLastError = true)]
        public static partial int CallWindowProcW(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [LibraryImport(LibraryName)]
        public static partial IntPtr LoadCursorW(IntPtr hInstance, int lpCursorName);

        [LibraryImport(LibraryName, SetLastError = true)]
        public static partial int PeekMessageW(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin, int wMsgFilterMax, int wRemoveMsg);

        [LibraryImport(LibraryName)]
        public static partial int DispatchMessageW(ref NativeMessage lpMsg);

        [LibraryImport(LibraryName)]
        public static partial int TranslateMessage(ref NativeMessage lpMsg);

        [LibraryImport(LibraryName)]
        public static partial int PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport(LibraryName)]
        public static partial int PostQuitMessage(int nExitCode);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UpdateWindow(IntPtr hWnd);

        [LibraryImport(LibraryName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DestroyWindow(IntPtr hWnd);

        [LibraryImport(LibraryName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool MoveWindow(IntPtr hWnd, uint X, uint Y, uint nWidth, uint nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

        [LibraryImport(LibraryName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string lpString);

        public static IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
            }

            return new IntPtr(SetWindowLong(hWnd, nIndex, dwNewLong.ToInt32()));
        }
        [LibraryImport(LibraryName, SetLastError = true, EntryPoint = "SetWindowLongW")]
        private static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [LibraryImport(LibraryName, SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
        private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr(hWnd, nIndex);
            }

            return new IntPtr(GetWindowLong(hWnd, nIndex));
        }
        [LibraryImport(LibraryName, SetLastError = true, EntryPoint = "GetWindowLongW")]
        private static partial IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
        [LibraryImport(LibraryName, SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [LibraryImport(LibraryName)]
        public static partial short GetKeyState(int nVirtKey);
        [LibraryImport(LibraryName)]
        public static partial short GetAsyncKeyState(int nVirtKey);

        [LibraryImport(LibraryName)]
        public static partial IntPtr SetCapture(IntPtr hWnd);
        [LibraryImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReleaseCapture();

        public static int GetWheelDeltaWParam(IntPtr wParam)
        {
            return (short)HIWORD(wParam);
        }
        public static IntPtr HIWORD(IntPtr l)
        {
            return (ushort)((l >> 16) & 0xffff);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public nint hwnd;
            public uint msg;
            public nuint wParam;
            public nint lParam;
            public uint time;
            public int ptx;
            public int pty;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public uint Left;
            public uint Top;
            public uint Right;
            public uint Bottom;

            public readonly uint GetWidth()
            {
                return Right - Left;
            }
            public readonly uint GetHeight()
            {
                return Bottom - Top;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEXW
        {
            public int cbSize;
            public ClassStyles style;
            public IntPtr lpfnWndProc; // not WndProc
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public IntPtr lpszMenuName;
            public IntPtr lpszClassName;
            public IntPtr hIconSm;

            public WNDCLASSEXW()
            {
                cbSize = Marshal.SizeOf<WNDCLASSEXW>();
            }
        }

        public readonly struct IDC_STANDARD_CURSORS
        {
            public const uint IDC_ARROW = 32512;
            public const uint IDC_IBEAM = 32513;
            public const uint IDC_WAIT = 32514;
            public const uint IDC_CROSS = 32515;
            public const uint IDC_UPARROW = 32516;
            public const uint IDC_SIZE = 32640;
            public const uint IDC_ICON = 32641;
            public const uint IDC_SIZENWSE = 32642;
            public const uint IDC_SIZENESW = 32643;
            public const uint IDC_SIZEWE = 32644;
            public const uint IDC_SIZENS = 32645;
            public const uint IDC_SIZEALL = 32646;
            public const uint IDC_NO = 32648;
            public const uint IDC_HAND = 32649;
            public const uint IDC_APPSTARTING = 32650;
            public const uint IDC_HELP = 32651;
        }

        [Flags]
        public enum ClassStyles : uint
        {
            /// <summary>
            /// Aligns the window's client area on a byte boundary (in the x direction). This style affects the width of the window and its horizontal placement on the display.
            /// </summary>
            ByteAlignClient = 0x1000,
            /// <summary>
            /// Aligns the window on a byte boundary (in the x direction). This style affects the width of the window and its horizontal placement on the display.
            /// </summary>
            ByteAlignWindow = 0x2000,
            /// <summary>
            /// Allocates one device context to be shared by all windows in the class.
            /// Because window classes are process specific, it is possible for multiple threads of an application to create a window of the same class.
            /// It is also possible for the threads to attempt to use the device context simultaneously. When this happens, the system allows only one thread to successfully finish its drawing operation.
            /// </summary>
            ClassDC = 0x40,
            /// <summary>
            /// Sends a double-click message to the window procedure when the user double-clicks the mouse while the cursor is within a window belonging to the class.
            /// </summary>
            DoubleClicks = 0x8,
            /// <summary>
            /// Enables the drop shadow effect on a window. The effect is turned on and off through SPI_SETDROPSHADOW.
            /// Typically, this is enabled for small, short-lived windows such as menus to emphasize their Z order relationship to other windows.
            /// </summary>
            DropShadow = 0x20000,
            /// <summary>
            /// Indicates that the window class is an application global class. For more information, see the "Application Global Classes" section of About Window Classes.
            /// </summary>
            GlobalClass = 0x4000,
            /// <summary>
            /// Redraws the entire window if a movement or size adjustment changes the width of the client area.
            /// </summary>
            HorizontalRedraw = 0x2,
            /// <summary>
            /// Disables Close on the window menu.
            /// </summary>
            NoClose = 0x200,
            /// <summary>
            /// Allocates a unique device context for each window in the class.
            /// </summary>
            OwnDC = 0x20,
            /// <summary>
            /// Sets the clipping rectangle of the child window to that of the parent window so that the child can draw on the parent.
            /// A window with the CS_PARENTDC style bit receives a regular device context from the system's cache of device contexts.
            /// It does not give the child the parent's device context or device context settings. Specifying CS_PARENTDC enhances an application's performance.
            /// </summary>
            ParentDC = 0x80,
            /// <summary>
            /// Saves, as a bitmap, the portion of the screen image obscured by a window of this class.
            /// When the window is removed, the system uses the saved bitmap to restore the screen image, including other windows that were obscured.
            /// Therefore, the system does not send WM_PAINT messages to windows that were obscured if the memory used by the bitmap has not been discarded and if other screen actions have not invalidated the stored image.
            /// This style is useful for small windows (for example, menus or dialog boxes) that are displayed briefly and then removed before other screen activity takes place.
            /// This style increases the time required to display the window, because the system must first allocate memory to store the bitmap.
            /// </summary>
            SaveBits = 0x800,
            /// <summary>
            /// Redraws the entire window if a movement or size adjustment changes the height of the client area.
            /// </summary>
            VerticalRedraw = 0x1
        }

        public readonly struct WindowStyles
        {
            /// <summary>
            /// The window has a thin-line border.
            /// </summary>  
            public const uint WS_BORDER = 0x800000;
            /// <summary>
            /// The window has a title bar (includes the WS_BORDER style).
            /// </summary>  
            public const uint WS_CAPTION = 0xc00000;
            /// <summary>
            /// The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the WS_POPUP style.
            /// </summary>  
            public const uint WS_CHILD = 0x40000000;
            /// <summary>
            /// Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when creating the parent window.
            /// </summary>  
            public const uint WS_CLIPCHILDREN = 0x2000000;
            /// <summary>  
            /// Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated.  
            /// If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window.  
            /// </summary>  
            public const uint WS_CLIPSIBLINGS = 0x4000000;
            /// <summary>
            /// The window is initially disabled. A disabled window cannot receive input from the user. To change this after a window has been created, use the EnableWindow function.
            /// </summary>  
            public const uint WS_DISABLED = 0x8000000;
            /// <summary>
            /// The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.
            /// </summary>  
            public const uint WS_DLGFRAME = 0x400000;
            /// <summary>  
            /// The window is the first control of a group of controls. The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style.  
            /// The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys.  
            /// You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.  
            /// </summary>  
            public const uint WS_GROUP = 0x20000;
            /// <summary>
            /// The window has a horizontal scroll bar.
            /// </summary>  
            public const uint WS_HSCROLL = 0x100000;
            /// <summary>
            /// The window is initially maximized.
            /// </summary>   
            public const uint WS_MAXIMIZE = 0x1000000;
            /// <summary>
            /// The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
            /// </summary>   
            public const uint WS_MAXIMIZEBOX = 0x10000;
            /// <summary>
            /// The window is initially minimized.
            /// </summary>  
            public const uint WS_MINIMIZE = 0x20000000;
            /// <summary>
            /// The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
            /// </summary>  
            public const uint WS_MINIMIZEBOX = 0x20000;
            /// <summary>
            /// The window is an overlapped window. An overlapped window has a title bar and a border.
            /// </summary>  
            public const uint WS_OVERLAPPED = 0x0;
            /// <summary>
            /// The window is an overlapped window.
            /// </summary>  
            public const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_SIZEFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
            /// <summary>
            /// The window is a pop-up window. This style cannot be used with the WS_CHILD style.
            /// </summary>  
            public const uint WS_POPUP = 0x80000000u;
            /// <summary>
            /// The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.
            /// </summary>  
            public const uint WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU;
            /// <summary>
            /// The window has a sizing border.
            /// </summary>  
            public const uint WS_SIZEFRAME = 0x40000;
            /// <summary>
            /// The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
            /// </summary>  
            public const uint WS_SYSMENU = 0x80000;
            /// <summary>  
            /// The window is a control that can receive the keyboard focus when the user presses the TAB key.  
            /// Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style.    
            /// You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.  
            /// For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.  
            /// </summary>  
            public const uint WS_TABSTOP = 0x10000;
            /// <summary>
            /// The window is initially visible. This style can be turned on and off by using the ShowWindow or SetWindowPos function.
            /// </summary>  
            public const uint WS_VISIBLE = 0x10000000;
            /// <summary>
            /// The window has a vertical scroll bar.
            /// </summary>  
            public const uint WS_VSCROLL = 0x20000;
        }

        public readonly struct WindowStylesEx
        {
            /// <summary>  
            /// Specifies that a window created with this style accepts drag-drop files.  
            /// </summary>  
            public const uint WS_EX_ACCEPTFILES = 0x00000010;
            /// <summary>  
            /// Forces a top-level window onto the taskbar when the window is visible.  
            /// </summary>  
            public const uint WS_EX_APPWINDOW = 0x00040000;
            /// <summary>  
            /// Specifies that a window has a border with a sunken edge.  
            /// </summary>  
            public const uint WS_EX_CLIENTEDGE = 0x00000200;
            /// <summary>  
            /// Windows XP: Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.   
            /// </summary>  
            public const uint WS_EX_COMPOSITED = 0x02000000;
            /// <summary>  
            /// Includes a question mark in the title bar of the window. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window.  
            /// WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.  
            /// </summary>  
            public const uint WS_EX_CONTEXTHELP = 0x00000400;
            /// <summary>  
            /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.  
            /// </summary>  
            public const uint WS_EX_CONTROLPARENT = 0x00010000;
            /// <summary>  
            /// Creates a window that has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.  
            /// </summary>  
            public const uint WS_EX_DLGMODALFRAME = 0x00000001;
            /// <summary>  
            /// Windows 2000/XP: Creates a layered window. Note that this cannot be used for child windows. Also, this cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC.   
            /// </summary>  
            public const uint WS_EX_LAYERED = 0x00080000;
            /// <summary>  
            /// Arabic and Hebrew versions of Windows 98/Me, Windows 2000/XP: Creates a window whose horizontal origin is on the right edge. Increasing horizontal values advance to the left.   
            /// </summary>  
            public const uint WS_EX_LAYOUTRTL = 0x00400000;
            /// <summary>  
            /// Creates a window that has generic left-aligned properties. This is the default.  
            /// </summary>  
            public const uint WS_EX_LEFT = 0x00000000;
            /// <summary>  
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.  
            /// </summary>  
            public const uint WS_EX_LEFTSCROLLBAR = 0x00004000;
            /// <summary>  
            /// The window text is displayed using left-to-right reading-order properties. This is the default.  
            /// </summary>  
            public const uint WS_EX_LTRREADING = 0x00000000;
            /// <summary>  
            /// Creates a multiple-document interface (MDI) child window.  
            /// </summary>  
            public const uint WS_EX_MDICHILD = 0x00000040;
            /// <summary>  
            /// Windows 2000/XP: A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.   
            /// To activate the window, use the SetActiveWindow or SetForegroundWindow function.  
            /// The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.  
            /// </summary>  
            public const uint WS_EX_NOACTIVATE = 0x08000000;
            /// <summary>  
            /// Windows 2000/XP: A window created with this style does not pass its window layout to its child windows.  
            /// </summary>  
            public const uint WS_EX_NOINHERITLAYOUT = 0x00100000;
            /// <summary>  
            /// Specifies that a child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.  
            /// </summary>  
            public const uint WS_EX_NOPARENTNOTIFY = 0x00000004;
            /// <summary>  
            /// Combines the WS_EX_CLIENTEDGE and WS_EX_WINDOWEDGE styles.  
            /// </summary>  
            public const uint WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
            /// <summary>  
            /// Combines the WS_EX_WINDOWEDGE, WS_EX_TOOLWINDOW, and WS_EX_TOPMOST styles.  
            /// </summary>  
            public const uint WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
            /// <summary>  
            /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored.  
            /// Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.  
            /// </summary>  
            public const uint WS_EX_RIGHT = 0x00001000;
            /// <summary>  
            /// Vertical scroll bar (if present) is to the right of the client area. This is the default.  
            /// </summary>  
            public const uint WS_EX_RIGHTSCROLLBAR = 0x00000000;
            /// <summary>  
            /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.  
            /// </summary>  
            public const uint WS_EX_RTLREADING = 0x00002000;
            /// <summary>  
            /// Creates a window with a three-dimensional border style intended to be used for items that do not accept user input.  
            /// </summary>  
            public const uint WS_EX_STATICEDGE = 0x00020000;
            /// <summary>  
            /// Creates a tool window; that is, a window intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE.   
            /// </summary>  
            public const uint WS_EX_TOOLWINDOW = 0x00000080;
            /// <summary>  
            /// Specifies that a window created with this style should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.  
            /// </summary>  
            public const uint WS_EX_TOPMOST = 0x00000008;
            /// <summary>  
            /// Specifies that a window created with this style should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted.  
            /// To achieve transparency without these restrictions, use the SetWindowRgn function.  
            /// </summary>  
            public const uint WS_EX_TRANSPARENT = 0x00000020;
            /// <summary>  
            /// Specifies that a window has a border with a raised edge.  
            /// </summary>  
            public const uint WS_EX_WINDOWEDGE = 0x0000010;
        }

        public readonly struct PEEK_MESSAGE_REMOVE_TYPE
        {
            public const uint PM_NOREMOVE = 0x00000000;
            public const uint PM_REMOVE = 0x00000001;
            public const uint PM_NOYIELD = 0x00000002;
            public const uint PM_QS_INPUT = 0x04070000;
            public const uint PM_QS_POSTMESSAGE = 0x00980000;
            public const uint PM_QS_PAINT = 0x00200000;
            public const uint PM_QS_SENDMESSAGE = 0x00400000;
        }

        public readonly struct WindowLongIndex
        {
            public const int GWL_EXSTYLE = -20;
            public const int GWL_HINSTANCE = -6;
            public const int GWL_ID = -12;
            public const int GWL_STYLE = -16;
            public const int GWL_USERDATA = -21;
            public const int GWL_WNDPROC = -4;
        }

        public readonly struct ShowWindowCommands
        {
            public const int SW_HIDE = 0;
            public const int SW_SHOWNORMAL = 1;
            public const int SW_NORMAL = 1;
            public const int SW_SHOWMINIMIZED = 2;
            public const int SW_SHOWMAXIMIZED = 3;
            public const int SW_MAXIMIZE = 3;
            public const int SW_SHOWNOACTIVATE = 4;
            public const int SW_SHOW = 5;
            public const int SW_MINIMIZE = 6;
            public const int SW_SHOWMINNOACTIVE = 7;
            public const int SW_SHOWNA = 8;
            public const int SW_RESTORE = 9;
            public const int SW_SHOWDEFAULT = 10;
            public const int SW_FORCEMINIMIZE = 11;
            public const int SW_MAX = 1;
        }

        public readonly struct WM_SIZE_WPARAM
        {
            public const uint SIZE_RESTORED = 0;
            public const uint SIZE_MINIMIZED = 1;
            public const uint SIZE_MAXIMIZED = 2;
            public const uint SIZE_MAXSHOW = 3;
            public const uint SIZE_MAXHIDE = 4;
        }

        public readonly struct WindowMessages
        {
            public const uint WM_NCCREATE = 0x0081;
            public const uint WM_DESTROY = 0x0002;
            public const uint WM_SIZE = 0x0005;
            public const uint WM_CLOSE = 0x0010;
            public const uint WM_QUIT = 0x0012;
            public const uint WM_KEYDOWN = 0x0100;
            public const uint WM_SYSCHAR = 0x0106;
            public const uint WM_SYSCOMMAND = 0x0112;
            public const uint WM_CAPTURECHANGED = 0x0215;
        }

        public readonly struct VirtualKeys
        {
            public const uint VK_LBUTTON = 0x01;
            public const uint VK_RETURN = 0x0D;
            public const uint VK_ESCAPE = 0x1B;
        }

        public readonly struct KeystrokeFlags
        {
            public const uint KF_ALTDOWN = 0x2000;
        }
    }
}
