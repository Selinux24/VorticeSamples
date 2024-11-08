using Engine.Platform;
using SharpGen.Runtime;
using System.Drawing;
using System.Runtime.CompilerServices;
using Vortice.DXGI;

namespace Engine
{
    public static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Format ToSwapChainFormat(Format format)
        {
            return format switch
            {
                Format.R16G16B16A16_Float => Format.R16G16B16A16_Float,
                Format.B8G8R8A8_UNorm or Format.B8G8R8A8_UNorm_SRgb => Format.B8G8R8A8_UNorm,
                Format.R8G8B8A8_UNorm or Format.R8G8B8A8_UNorm_SRgb => Format.R8G8B8A8_UNorm,
                Format.R10G10B10A2_UNorm => Format.R10G10B10A2_UNorm,
                _ => Format.B8G8R8A8_UNorm,
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDXGISwapChain1 CreateSwapChain(Window window, IDXGIFactory2 factory, ComObject deviceOrCommandQueue, Format colorFormat)
        {
            SizeF size = window.ClientSize;
            Format backBufferFormat = ToSwapChainFormat(colorFormat);

            bool isTearingSupported = false;
            using (IDXGIFactory5 factory5 = factory.QueryInterfaceOrNull<IDXGIFactory5>())
            {
                if (factory5 != null)
                {
                    isTearingSupported = factory5.PresentAllowTearing;
                }
            }

            SwapChainDescription1 desc = new()
            {
                Width = (int)size.Width,
                Height = (int)size.Height,
                Format = backBufferFormat,
                BufferCount = window.BackBufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = SampleDescription.Default,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore,
                Flags = isTearingSupported ? SwapChainFlags.AllowTearing : SwapChainFlags.None
            };

            SwapChainFullscreenDescription fullscreenDesc = new()
            {
                Windowed = true
            };

            var hwnd = window.Handle;
            var swapChain = factory.CreateSwapChainForHwnd(deviceOrCommandQueue, hwnd, desc, fullscreenDesc);
            factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);
            return swapChain;
        }
    }
}
