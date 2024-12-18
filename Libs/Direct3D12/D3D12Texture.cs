using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12Texture
    {
        public const int MaxMips = 14;

        private readonly D3D12Graphics graphics;
        private ID3D12Resource resource;
        private D3D12DescriptorHandle srv;

        public ID3D12Resource Resource { get => resource; }
        public D3D12DescriptorHandle Srv { get => srv; }

        public D3D12Texture(D3D12Graphics graphics)
        {
            this.graphics = graphics;
        }
        public D3D12Texture(D3D12Graphics graphics, D3D12TextureInitInfo info)
        {
            this.graphics = graphics;

            var device = graphics.Device;
            Debug.Assert(device != null);

            ClearValue clearValue = (info.Desc.Flags.HasFlag(ResourceFlags.AllowRenderTarget) || info.Desc.Flags.HasFlag(ResourceFlags.AllowDepthStencil)) ?
                info.ClearValue :
                default;

            if (info.Resource != null)
            {
                Debug.Assert(info.Heap == null);
                resource = info.Resource;
            }
            else if (info.Heap != null)
            {
                Debug.Assert(info.Resource == null);

                if (!device.CreatePlacedResource(
                    info.Heap,
                    info.AllocationInfo.Offset,
                    info.Desc,
                    info.InitialState,
                    clearValue,
                    out resource).Success)
                {
                    Debug.WriteLine("Failed to create placed resource");
                }
            }
            else
            {
                Debug.Assert(info.Heap == null && info.Resource == null);

                if (!device.CreateCommittedResource(
                    D3D12Helpers.DefaultHeap,
                    HeapFlags.None,
                    info.Desc,
                    info.InitialState,
                    clearValue,
                    out resource).Success)
                {
                    Debug.WriteLine("Failed to create committed resource");
                }
            }

            Debug.Assert(resource != null);
            srv = graphics.SrvHeap.Allocate();

            device.CreateShaderResourceView(resource, info.SrvDesc, srv.Cpu);
        }
        public D3D12Texture(D3D12Texture o)
        {
            graphics = o.graphics;
            resource = o.Resource;
            srv = o.Srv;

            o.Reset();
        }
        ~D3D12Texture()
        {
            Release();
        }

        public void Release()
        {
            graphics.SrvHeap.Free(ref srv);
            graphics.DeferredRelease(resource);
        }

        private void Reset()
        {
            resource = null;
            srv = default;
        }
    }
}
