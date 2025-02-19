using PrimalLike.Graphics;
using PrimalLike.Platform;
using PrimalLikeDLL.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PrimalLikeDLL
{
    public partial class EngineAPI
    {
        private delegate IntPtr ScriptCreatorDelegate(string name);
        private delegate string[] GetScriptNamesDelegate();

        private static IntPtr gameCodeDll;
        private static IPlatform platform;
        private static ScriptCreatorDelegate getScriptCreator;
        private static GetScriptNamesDelegate getScriptNames;
        private static readonly List<RenderSurface> surfaces = [];

        public static void SetPlatform(IPlatform platform)
        {
            EngineAPI.platform = platform;
        }

        public static bool LoadGameCodeDll(string dllPath)
        {
            if (gameCodeDll != IntPtr.Zero)
            {
                return false;
            }

            gameCodeDll = Kernel32.LoadLibrary(dllPath);
            if (gameCodeDll == IntPtr.Zero)
            {
                return false;
            }

            IntPtr scriptCreatorPtr = Kernel32.GetProcAddress(gameCodeDll, "get_script_creator");
            IntPtr scriptNamesPtr = Kernel32.GetProcAddress(gameCodeDll, "get_script_names");

            if (scriptCreatorPtr == IntPtr.Zero || scriptNamesPtr == IntPtr.Zero)
            {
                return false;
            }

            getScriptCreator = Marshal.GetDelegateForFunctionPointer<ScriptCreatorDelegate>(scriptCreatorPtr);
            getScriptNames = Marshal.GetDelegateForFunctionPointer<GetScriptNamesDelegate>(scriptNamesPtr);

            return true;
        }
        public static bool UnloadGameCodeDll()
        {
            if (gameCodeDll == IntPtr.Zero)
            {
                return false;
            }
            bool result = Kernel32.FreeLibrary(gameCodeDll);
            if (!result)
            {
                return false;
            }

            gameCodeDll = IntPtr.Zero;
            return true;
        }

        public static IntPtr GetScriptCreator(string name)
        {
            if (gameCodeDll == IntPtr.Zero || getScriptCreator == null)
            {
                return IntPtr.Zero;
            }

            return getScriptCreator(name);
        }

        public static string[] GetScriptNames()
        {
            if (gameCodeDll == IntPtr.Zero || getScriptNames == null)
            {
                return null;
            }

            return getScriptNames();
        }

        public static int CreateRenderSurface(IntPtr host, IPlatformWindowInfo info)
        {
            if (host == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var surface = new RenderSurface()
            {
                Window = platform.CreateWindow(info),
                Surface = default,
            };

            surfaces.Add(surface);
            return surfaces.Count - 1;
        }

        public static void RemoveRenderSurface(int id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, surfaces.Count);

            surfaces[id] = default;
        }

        public static IntPtr GetWindowHandle(int id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, surfaces.Count);

            return surfaces[id].Window.Handle;
        }

        public static void ResizeRenderSurface(int id)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, surfaces.Count);

            surfaces[id].Window.Resize(0, 0);
        }
    }
}
