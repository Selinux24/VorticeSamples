using Vortice.Direct3D12;

namespace Direct3D12
{
    readonly struct D3D12CommandSignatureDesc
    {
        public static ID3D12CommandSignature Create(int byteStride, IndirectArgumentDescription[] args, ID3D12RootSignature rootSignature)
        {
            return D3D12Helpers.CreateCommandSignature(new(byteStride, args, 0), rootSignature);
        }
    }
}
