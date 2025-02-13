using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace Direct3D12.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    struct LightCullingDispatchParameters()
    {
        public UInt2 NumThreadGroups;
        public UInt2 NumThreads;
        public uint NumLights;
        public uint DepthBufferSrvIndex;
    }
}
