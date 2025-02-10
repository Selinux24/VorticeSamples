using PrimalLike;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using PrimalLike.Platform;
using System;
using System.Diagnostics;
using static Native32.User32;

namespace WindowsPlatform
{
    public class Win32Platform : IPlatform
    {
        /// <inheritdoc/>
        public Window CreateWindow(IPlatformWindowInfo info)
        {
            return Win32PlatformBase.CreateWindow((Win32WindowInfo)info);
        }
        /// <inheritdoc/>
        public void RemoveWindow(uint id)
        {
            Win32PlatformBase.RemoveWindow(id);
        }

        /// <inheritdoc/>
        public void SetFullscreen(uint id, bool isFullscreen)
        {
            Debug.Assert(IdDetail.IsValid(id));
            Win32PlatformBase.SetWindowFullscreen(id, isFullscreen);
        }
        /// <inheritdoc/>
        public bool IsFullscreen(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.IsWindowFullscreen(id);
        }
        /// <inheritdoc/>
        public nint GetWindowHandle(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.GetWindowHandle(id);
        }
        /// <inheritdoc/>
        public void SetCaption(uint id, string caption)
        {
            Debug.Assert(IdDetail.IsValid(id));
            Win32PlatformBase.SetWindowCaption(id, caption);
        }
        /// <inheritdoc/>
        public ClientArea Size(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.GetWindowSize(id);
        }
        /// <inheritdoc/>
        public void Resize(uint id, uint width, uint height)
        {
            Debug.Assert(IdDetail.IsValid(id));
            Win32PlatformBase.ResizeWindow(id, width, height);
        }
        /// <inheritdoc/>
        public uint GetHeight(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.GetWindowSize(id).Height;
        }
        /// <inheritdoc/>
        public uint GetWidth(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.GetWindowSize(id).Width;
        }
        /// <inheritdoc/>
        public bool IsClosed(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            return Win32PlatformBase.IsWindowClosed(id);
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

            Win32PlatformBase.UnregisterWindow();
        }
    }
}
