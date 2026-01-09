using System;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12RenderTexture : IDisposable
    {
        readonly D3D12Texture texture;
        readonly DescriptorHandle[] rtv = new DescriptorHandle[D3D12Texture.MaxMips];

        public int MipCount { get; private set; }
        public DescriptorHandle GetSrv() { return texture.Srv; }
        public ID3D12Resource GetResource() { return texture.Resource; }

        public D3D12RenderTexture(D3D12TextureInitInfo info)
        {
            texture = new D3D12Texture(info);

            MipCount = texture.Resource.Description.MipLevels;
            Debug.Assert(MipCount != 0 && MipCount <= D3D12Texture.MaxMips);

            Debug.Assert(info.Desc != null);
            DescriptorHeap rtvHeap = D3D12Graphics.RtvHeap;
            RenderTargetViewDescription desc = new()
            {
                Format = info.Desc.Value.Format,
                ViewDimension = RenderTargetViewDimension.Texture2D
            };
            desc.Texture2D.MipSlice = 0;

            var device = D3D12Graphics.Device;
            Debug.Assert(device != null);

            for (uint i = 0; i < MipCount; i++)
            {
                rtv[i] = rtvHeap.Allocate();
                device.CreateRenderTargetView(texture.Resource, desc, rtv[i].Cpu);
                desc.Texture2D.MipSlice++;
            }
        }
        public D3D12RenderTexture(D3D12RenderTexture o)
        {
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
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Release();
            }
        }
        void Release()
        {
            for (uint i = 0; i < MipCount; i++)
            {
                D3D12Graphics.RtvHeap.Free(ref rtv[i]);
            }
            texture.Dispose();
            MipCount = 0;
        }

        public DescriptorHandle GetRtv(int mipIndex)
        {
            return rtv[mipIndex];
        }

        void Reset()
        {
            for (int i = 0; i < MipCount; i++)
            {
                rtv[i] = default;
            }

            MipCount = 0;
        }
    }
}
