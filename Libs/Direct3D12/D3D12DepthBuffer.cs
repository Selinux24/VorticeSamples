using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    class D3D12DepthBuffer : IDisposable
    {
        private readonly D3D12Texture texture;
        private D3D12DescriptorHandle dsv;

        public D3D12DescriptorHandle Dsv { get => dsv; }
        public ID3D12Resource Resource { get => texture.Resource; }

        public D3D12DepthBuffer()
        {
        }
        public D3D12DepthBuffer(D3D12TextureInitInfo info)
        {
            Debug.Assert(info.Desc != null);
            var desc = info.Desc.Value;

            Format srvFormat = desc.Format;
            Format dsvFormat = desc.Format;

            if (desc.Format == Format.D32_Float)
            {
                desc.Format = Format.R32_Typeless;
                srvFormat = Format.R32_Float;
            }
            info.Desc = desc;

            ShaderResourceViewDescription srvDesc = new()
            {
                Shader4ComponentMapping = ShaderComponentMapping.Default,
                Format = srvFormat,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
            };
            srvDesc.Texture2D.MipLevels = 1;
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Texture2D.PlaneSlice = 0;
            srvDesc.Texture2D.ResourceMinLODClamp = 0.0f;

            Debug.Assert(info.Resource == null);
            info.SrvDesc = srvDesc;
            texture = new D3D12Texture(info);

            DepthStencilViewDescription dsvDesc = new()
            {
                ViewDimension = DepthStencilViewDimension.Texture2D,
                Flags = DepthStencilViewFlags.None,
                Format = dsvFormat,
            };
            dsvDesc.Texture2D.MipSlice = 0;

            dsv = D3D12Graphics.DsvHeap.Allocate();

            var device = D3D12Graphics.Device;
            Debug.Assert(device != null);
            device.CreateDepthStencilView(Resource, dsvDesc, dsv.Cpu);
        }
        public D3D12DepthBuffer(D3D12DepthBuffer o)
        {
            texture = o.texture;
            dsv = o.dsv;
            o.dsv = default;
        }
        ~D3D12DepthBuffer()
        {
            Release();
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Release();
            }
        }
        private void Release()
        {
            D3D12Graphics.DsvHeap.Free(ref dsv);
            texture.Dispose();
        }
    }
}
