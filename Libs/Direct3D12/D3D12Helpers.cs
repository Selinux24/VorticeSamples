﻿global using D3D12Device = Vortice.Direct3D12.ID3D12Device8;
global using D3D12GraphicsCommandList = Vortice.Direct3D12.ID3D12GraphicsCommandList6;
global using DXGIAdapter = Vortice.DXGI.IDXGIAdapter4;
global using DXGIFactory = Vortice.DXGI.IDXGIFactory7;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Direct3D12
{
    using HResult = SharpGen.Runtime.Result;

    public static unsafe class D3D12Helpers
    {
        #region Structured Collections

        public readonly struct HeapPropertiesCollection()
        {
            private static readonly HeapProperties defaultHeap = new(
                HeapType.Default,
                CpuPageProperty.Unknown,
                MemoryPool.Unknown,
                0,
                0);

            private static readonly HeapProperties uploadHeap = new(
                HeapType.Upload,
                CpuPageProperty.Unknown,
                MemoryPool.Unknown,
                0,
                0);

            public static HeapProperties DefaultHeap { get => defaultHeap; }
            public static HeapProperties UploadHeap { get => uploadHeap; }
        }
        public readonly struct RasterizerStatesCollection()
        {
            private static readonly RasterizerDescription noCull = new(
                CullMode.None,
                FillMode.Solid,
                true,
                0,
                0,
                0,
                true,
                false,
                false,
                0,
                ConservativeRasterizationMode.Off);

            private static readonly RasterizerDescription backFaceCull = new(
                CullMode.Back,
                FillMode.Solid,
                true,
                0,
                0,
                0,
                true,
                false,
                false,
                0,
                ConservativeRasterizationMode.Off);

            private static readonly RasterizerDescription frontFaceCull = new(
                CullMode.Front,
                FillMode.Solid,
                true,
                0,
                0,
                0,
                true,
                false,
                false,
                0,
                ConservativeRasterizationMode.Off);

            private static readonly RasterizerDescription wireframe = new(
                CullMode.None,
                FillMode.Wireframe,
                true,
                0,
                0,
                0,
                true,
                false,
                false,
                0,
                ConservativeRasterizationMode.Off);

            public static RasterizerDescription NoCull { get => noCull; }
            public static RasterizerDescription BackFaceCull { get => backFaceCull; }
            public static RasterizerDescription FrontFaceCull { get => frontFaceCull; }
            public static RasterizerDescription Wireframe { get => wireframe; }
        }
        public readonly struct DepthStatesCollection()
        {
            private static readonly DepthStencilDescription1 disabled = new(
                false,
                DepthWriteMask.Zero,
                ComparisonFunction.LessEqual);
            private static readonly DepthStencilDescription1 enabled = new(
                true,
                DepthWriteMask.All,
                ComparisonFunction.LessEqual);
            private static readonly DepthStencilDescription1 enabledReadonly = new(
                true,
                DepthWriteMask.Zero,
                ComparisonFunction.LessEqual);
            private static readonly DepthStencilDescription1 reversed = new(
                true,
                DepthWriteMask.All,
                ComparisonFunction.GreaterEqual);
            private static readonly DepthStencilDescription1 reversedReadonly = new(
                true,
                DepthWriteMask.Zero,
                ComparisonFunction.GreaterEqual);

            public static DepthStencilDescription1 Disabled { get => disabled; }
            public static DepthStencilDescription1 Enabled { get => enabled; }
            public static DepthStencilDescription1 EnabledReadonly { get => enabledReadonly; }
            public static DepthStencilDescription1 Reversed { get => reversed; }
            public static DepthStencilDescription1 ReversedReadonly { get => reversedReadonly; }
        }
        public readonly struct BlendStatesCollection()
        {
            private static readonly BlendDescription disabled = new(
                Blend.SourceAlpha,
                Blend.InverseSourceAlpha,
                Blend.One,
                Blend.One);

            public static BlendDescription Disabled { get => disabled; }
        }
        public readonly struct SampleStatesCollection()
        {
            private static readonly StaticSamplerDescription staticPoint = new(
                new SamplerDescription(
                    Filter.MinMagMipPoint,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    0f,
                    1,
                    ComparisonFunction.Always,
                    new Color4(0f, 0f, 0f, 1f),
                    0f, float.MaxValue),
                ShaderVisibility.Pixel, 0, 0);
            private static readonly StaticSamplerDescription staticLinear = new(
                new SamplerDescription(
                    Filter.MinMagMipLinear,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    0f,
                    1,
                    ComparisonFunction.Always,
                    new Color4(0f, 0f, 0f, 1f),
                    0f, float.MaxValue),
                ShaderVisibility.Pixel, 0, 0);
            private static readonly StaticSamplerDescription staticAnisotropic = new(
                new SamplerDescription(
                    Filter.Anisotropic,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    TextureAddressMode.Wrap,
                    0f,
                    1,
                    ComparisonFunction.Always,
                    new Color4(0f, 0f, 0f, 1f),
                    0f, float.MaxValue),
                ShaderVisibility.Pixel, 0, 0);

            public static StaticSamplerDescription StaticPoint { get => staticPoint; }
            public static StaticSamplerDescription StaticLinear { get => staticLinear; }
            public static StaticSamplerDescription StaticAnisotropic { get => staticAnisotropic; }
        }

        #endregion

        private const string SEPARATOR_BEGIN = "** ERROR on {0} *********************************************";
        private const string SEPARATOR_END = "** {0} ******************************************************";

        public static bool DxCall(HResult result)
        {
            if (result.Success)
            {
                return true;
            }

            Debug.WriteLine($"DirectX call failed: {result.Description}");

            if (D3D12Graphics.Device != null)
            {
                var removedReason = D3D12Graphics.Device.DeviceRemovedReason;
                if (!removedReason.Success)
                {
                    Debug.WriteLine($"Device removed: {removedReason.Description}");
                }
            }

#if DEBUG
            Debugger.Break();
#endif

            return false;
        }

        public static void NameD3D12Object(ID3D12Object obj, string name)
        {
            obj.Name = name;
            Debug.WriteLine($"D3D12 Object Created: {name}");
        }
        public static void NameD3D12Object(ID3D12Object obj, ulong index, string name)
        {
            string fullName = $"{name}[{index}]";
            obj.Name = fullName;
            Debug.WriteLine($"D3D12 Object Created: {fullName}");
        }
        public static void NameD3D12Object(ID3D12Object obj, long index, string name)
        {
            string fullName = $"{name}[{index}]";
            obj.Name = fullName;
            Debug.WriteLine($"D3D12 Object Created: {fullName}");
        }
        public static void NameD3D12Object(ID3D12Object obj, uint index, string name)
        {
            NameD3D12Object(obj, (ulong)index, name);
        }
        public static void NameD3D12Object(ID3D12Object obj, int index, string name)
        {
            NameD3D12Object(obj, (long)index, name);
        }
        public static void NameD3D12Object(ID3D12Object obj, ushort index, string name)
        {
            NameD3D12Object(obj, (ulong)index, name);
        }
        public static void NameD3D12Object(ID3D12Object obj, short index, string name)
        {
            NameD3D12Object(obj, (long)index, name);
        }

        public static void NameDXGIObject(IDXGIObject obj, string name)
        {
            obj.DebugName = name;
            Debug.WriteLine($"D3D12 Object Created: {name}");
        }

        public static DescriptorRange1 Range(
            DescriptorRangeType rangeType,
            uint descriptorCount,
            uint shaderRegister,
            uint space = 0,
            DescriptorRangeFlags flags = DescriptorRangeFlags.None,
            uint offsetFromTableStart = D3D12.DescriptorRangeOffsetAppend)
        {
            return new()
            {
                RangeType = rangeType,
                NumDescriptors = descriptorCount,
                BaseShaderRegister = shaderRegister,
                RegisterSpace = space,
                Flags = flags,
                OffsetInDescriptorsFromTableStart = offsetFromTableStart,
            };
        }

        public static RootParameter1 AsConstants(
            uint numConstants,
            ShaderVisibility visibility,
            uint shaderRegister,
            uint space = 0)
        {
            RootConstants rootConstants = new(shaderRegister, space, numConstants);

            return new(rootConstants, visibility);
        }

        private static RootParameter1 AsDescriptor(
            RootParameterType type,
            ShaderVisibility visibility,
            uint shaderRegister,
            uint space,
            RootDescriptorFlags flags)
        {
            RootDescriptor1 rootDescriptor = new(shaderRegister, space, flags);

            return new(type, rootDescriptor, visibility);
        }

        public static RootParameter1 AsCbv(
            ShaderVisibility visibility,
            uint shaderRegister,
            uint space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.ConstantBufferView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsSrv(
            ShaderVisibility visibility,
            uint shaderRegister,
            uint space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.ShaderResourceView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsUav(
            ShaderVisibility visibility,
            uint shaderRegister,
            uint space = 0,
            RootDescriptorFlags flags = RootDescriptorFlags.None)
        {
            return AsDescriptor(RootParameterType.UnorderedAccessView, visibility, shaderRegister, space, flags);
        }

        public static RootParameter1 AsDescriptorTable(
            ShaderVisibility visibility,
            DescriptorRange1[] ranges)
        {
            RootDescriptorTable1 table = new(ranges);

            return new(table, visibility);
        }

        public static ID3D12RootSignature CreateRootSignature(RootSignatureDescription1 desc)
        {
            VersionedRootSignatureDescription versionedDesc = new(desc);

            string errorMsg = D3D12.D3D12SerializeVersionedRootSignature(versionedDesc, out var signatureBlob);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                const string caller = nameof(D3D12.D3D12SerializeVersionedRootSignature);
                Debug.WriteLine(SEPARATOR_BEGIN, [caller]);
                Debug.WriteLine(errorMsg);
                Debug.WriteLine(SEPARATOR_END, [caller]);

                return null;
            }

            if (!DxCall(D3D12Graphics.Device.CreateRootSignature(0, signatureBlob.BufferPointer, signatureBlob.BufferSize, out ID3D12RootSignature signature)))
            {
                signature.Dispose();
            }

            return signature;
        }

        public static ID3D12Resource CreateBuffer<T>(
            T[] data, uint bufferSize,
            bool isCpuAccessible = false,
            ResourceStates state = ResourceStates.Common,
            ResourceFlags flags = ResourceFlags.None,
            ID3D12Heap heap = null, ulong heapOffset = 0) where T : unmanaged
        {
            Debug.Assert(bufferSize > 0);

            ResourceDescription desc = new()
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = bufferSize,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.Unknown,
                SampleDescription = new(1, 0),
                Layout = TextureLayout.RowMajor,
                Flags = isCpuAccessible ? ResourceFlags.None : flags
            };

            // The buffer will be only used for upload or as constant buffer/UAV.
            Debug.Assert(desc.Flags == ResourceFlags.None || desc.Flags == ResourceFlags.AllowUnorderedAccess);

            ID3D12Resource resource = null;
            ResourceStates resourceState = isCpuAccessible ? ResourceStates.GenericRead : state;

            if (heap != null)
            {
                DxCall(D3D12Graphics.Device.CreatePlacedResource(
                    heap, heapOffset,
                    desc,
                    resourceState,
                    out resource));
            }
            else
            {
                DxCall(D3D12Graphics.Device.CreateCommittedResource(
                    isCpuAccessible ? HeapPropertiesCollection.UploadHeap : HeapPropertiesCollection.DefaultHeap,
                    HeapFlags.None,
                    desc,
                    resourceState,
                    out resource));
            }

            if (data != null)
            {
                // If we have initial data which we'd like to be able to change later, we set is_cpu_accessible
                // to true. If we only want to upload some data once to be used by the GPU, then is_cpu_accessible
                // should be set to false.
                if (isCpuAccessible)
                {
                    // NOTE: range's Begin and End fields are set to 0, to indicate that
                    //       the CPU is not reading any data (i.e. write-only)
                    void* cpuAddress = default;
                    DxCall(resource.Map(0, cpuAddress));
                    Debug.Assert(cpuAddress != null);
                    uint sizeInBytes = (uint)(sizeof(T) * data.Length);
                    fixed (T* dataPtr = data)
                    {
                        NativeMemory.Copy(dataPtr, cpuAddress, sizeInBytes);
                    }
                    resource.Unmap(0, null);
                }
                else
                {
                    D3D12Upload.UploadContext context = new(bufferSize);
                    uint sizeInBytes = (uint)(sizeof(T) * data.Length);
                    fixed (T* dataPtr = data)
                    {
                        NativeMemory.Copy(dataPtr, context.CpuAddress, sizeInBytes);
                    }
                    context.CmdList.CopyResource(resource, context.UploadBuffer);
                    context.EndUpload();
                    context = null;
                }
            }

            Debug.Assert(resource != null);
            return resource;
        }

        public static void DeferredRelease(ID3D12Resource resource)
        {
            D3D12Graphics.DeferredRelease(resource);
        }

        public static ulong AlignSizeForConstantBuffer(ulong size)
        {
            return MathHelper.AlignUp(size, D3D12.ConstantBufferDataPlacementAlignment);
        }

        public static ulong AlignSizeForTexture(ulong size)
        {
            return MathHelper.AlignUp(size, D3D12.TextureDataPlacementAlignment);
        }

        public static StaticSamplerDescription StaticSampler(StaticSamplerDescription staticSampler, uint shaderRegister, uint registerSpace, ShaderVisibility visibility)
        {
            var sampler = staticSampler;
            sampler.ShaderRegister = shaderRegister;
            sampler.RegisterSpace = registerSpace;
            sampler.ShaderVisibility = visibility;

            return sampler;
        }
    }
}