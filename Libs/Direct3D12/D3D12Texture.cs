using System;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12Texture : IDisposable
    {
        public const int MaxMips = 14;

        private ID3D12Resource resource;
        private D3D12DescriptorHandle srv;

        public ID3D12Resource Resource { get => resource; }
        public D3D12DescriptorHandle Srv { get => srv; }

        public D3D12Texture()
        {
        }
        public D3D12Texture(D3D12TextureInitInfo info)
        {
            var device = D3D12Graphics.Device;
            Debug.Assert(device != null);

            Debug.Assert(info.Desc != null);
            var desc = info.Desc.Value;
            ClearValue clearValue = (desc.Flags.HasFlag(ResourceFlags.AllowRenderTarget) || desc.Flags.HasFlag(ResourceFlags.AllowDepthStencil)) ?
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

                if (!D3D12Helpers.DxCall(device.CreatePlacedResource(
                    info.Heap,
                    info.AllocationInfo.Offset,
                    desc,
                    info.InitialState,
                    clearValue,
                    out resource)))
                {
                    Debug.WriteLine("Failed to create placed resource");
                }
            }
            else
            {
                Debug.Assert(info.Heap == null && info.Resource == null);

                if (!D3D12Helpers.DxCall(device.CreateCommittedResource(
                    D3D12Helpers.HeapPropertiesCollection.DefaultHeap,
                    HeapFlags.None,
                    desc,
                    info.InitialState,
                    clearValue,
                    out resource)))
                {
                    Debug.WriteLine("Failed to create committed resource.");
                }
            }

            Debug.Assert(resource != null);
            srv = D3D12Graphics.SrvHeap.Allocate();

            device.CreateShaderResourceView(resource, info.SrvDesc, srv.Cpu);
        }
        public D3D12Texture(D3D12Texture o)
        {
            resource = o.Resource;
            srv = o.Srv;

            o.Reset();
        }
        ~D3D12Texture()
        {
            Dispose(false);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Release();
        }
        private void Release()
        {
            D3D12Graphics.SrvHeap.Free(ref srv);
            D3D12Graphics.DeferredRelease(resource);
        }

        private void Reset()
        {
            resource = null;
            srv = default;
        }
    }
}
