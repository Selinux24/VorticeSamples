using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Direct3D12
{
    class D3D12DepthBuffer
    {
        private readonly D3D12Graphics graphics;
        private readonly D3D12Texture texture;
        private D3D12DescriptorHandle dsv;

        public D3D12DescriptorHandle Dsv { get => dsv; }
        public ID3D12Resource Resource { get => texture.Resource; }

        public D3D12DepthBuffer(D3D12Graphics graphics)
        {
            this.graphics = graphics;
        }
        public D3D12DepthBuffer(D3D12Graphics graphics, D3D12TextureInitInfo info)
        {
            this.graphics = graphics;

            Format dsvFormat = info.Desc.Format;

            ShaderResourceViewDescription srvDesc = new();
            if (info.Desc.Format == Format.D32_Float)
            {
                info.Desc.Format = Format.R32_Typeless;
                srvDesc.Format = Format.R32_Float;
            }

            srvDesc.Shader4ComponentMapping = ShaderComponentMapping.Default;
            srvDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
            srvDesc.Texture2D.MipLevels = 1;
            srvDesc.Texture2D.MostDetailedMip = 0;
            srvDesc.Texture2D.PlaneSlice = 0;
            srvDesc.Texture2D.ResourceMinLODClamp = 0.0f;

            Debug.Assert(info.Resource == null);
            info.SrvDesc = srvDesc;
            texture = new D3D12Texture(graphics, info);

            DepthStencilViewDescription dsvDesc = new()
            {
                ViewDimension = DepthStencilViewDimension.Texture2D,
                Flags = DepthStencilViewFlags.None,
                Format = dsvFormat
            };
            dsvDesc.Texture2D.MipSlice = 0;

            dsv = graphics.DsvHeap.Allocate();

            var device = graphics.Device;
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

        public void Release()
        {
            graphics.DsvHeap.Free(ref dsv);
            texture.Release();
        }
    }
}
