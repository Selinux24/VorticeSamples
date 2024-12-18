using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12RenderTexture
    {
        private readonly D3D12Graphics graphics;
        private readonly D3D12Texture texture;
        private readonly D3D12DescriptorHandle[] rtv = new D3D12DescriptorHandle[D3D12Texture.MaxMips];

        public int MipCount { get; private set; }
        public D3D12DescriptorHandle Srv { get => texture.Srv; }
        public ID3D12Resource Resource { get => texture.Resource; }

        public D3D12RenderTexture(D3D12Graphics graphics)
        {
            this.graphics = graphics;
        }
        public D3D12RenderTexture(D3D12Graphics graphics, D3D12TextureInitInfo info)
        {
            this.graphics = graphics;

            texture = new D3D12Texture(graphics, info);

            MipCount = Resource.Description.MipLevels;
            Debug.Assert(MipCount != 0 && MipCount <= D3D12Texture.MaxMips);

            D3D12DescriptorHeap rtvHeap = graphics.RtvHeap;
            RenderTargetViewDescription desc = new()
            {
                Format = info.Desc.Format,
                ViewDimension = RenderTargetViewDimension.Texture2D
            };
            desc.Texture2D.MipSlice = 0;

            var device = graphics.Device;
            Debug.Assert(device != null);

            for (uint i = 0; i < MipCount; i++)
            {
                rtv[i] = rtvHeap.Allocate();
                device.CreateRenderTargetView(Resource, desc, rtv[i].Cpu);
                ++desc.Texture2D.MipSlice;
            }
        }
        public D3D12RenderTexture(D3D12RenderTexture o)
        {
            graphics = o.graphics;
            texture = o.texture;
            MipCount = o.MipCount;

            for (int i = 0; i < MipCount; i++)
            {
                rtv[i] = o.rtv[i];
            }

            o.Reset();
        }
        ~D3D12RenderTexture()
        {
            Release();
        }

        public void Release()
        {
            for (uint i = 0; i < MipCount; i++)
            {
                graphics.RtvHeap.Free(ref rtv[i]);
            }
            texture.Release();
            MipCount = 0;
        }

        public CpuDescriptorHandle GetRtv(int mipIndex)
        {
            return rtv[mipIndex].Cpu;
        }

        private void Reset()
        {
            for (int i = 0; i < MipCount; i++)
            {
                rtv[i] = default;
            }

            MipCount = 0;
        }
    }
}
