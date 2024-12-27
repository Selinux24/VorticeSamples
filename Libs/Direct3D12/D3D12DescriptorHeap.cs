using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12DescriptorHeap : IDisposable
    {
        private readonly DescriptorHeapType type;
        private readonly Mutex mutex;
        private readonly List<uint>[] deferredFreeIndices;

        private ID3D12DescriptorHeap heap;
        private CpuDescriptorHandle cpuStart;
        private GpuDescriptorHandle gpuStart;
        private uint[] freeHandles;
        private int capacity = 0;
        private int size = 0;
        private uint descriptorSize;

        public DescriptorHeapType DescriptorType { get => type; }
        public CpuDescriptorHandle CpuStart { get => cpuStart; }
        public GpuDescriptorHandle GpuStart { get => gpuStart; }
        public ID3D12DescriptorHeap Heap { get => heap; }
        public int Capacity { get => capacity; }
        public int Size { get => size; }
        public int DescriptorSize { get => (int)descriptorSize; }
        public bool IsShaderVisible { get => gpuStart.Ptr != 0; }

        public D3D12DescriptorHeap(DescriptorHeapType type)
        {
            this.type = type;

            mutex = new();

            deferredFreeIndices = new List<uint>[D3D12Graphics.FrameBufferCount];
            for (int i = 0; i < D3D12Graphics.FrameBufferCount; i++)
            {
                deferredFreeIndices[i] = [];
            }
        }
        ~D3D12DescriptorHeap()
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
            if (disposing)
            {
                Release();
            }
        }
        private void Release()
        {
            if (heap == null)
            {
                return;
            }

            Debug.Assert(size == 0);
            D3D12Graphics.DeferredRelease(heap);
            heap = null;
        }

        public bool Initialize(int capacity, bool isShaderVisible)
        {
            lock (mutex)
            {
                Debug.Assert(capacity != 0 && capacity < D3D12.MaxShaderVisibleDescriptorHeapSizeTier2);
                Debug.Assert(!(type == DescriptorHeapType.Sampler && capacity > D3D12.MaxShaderVisibleSamplerHeapSize));

                if (type == DescriptorHeapType.DepthStencilView ||
                    type == DescriptorHeapType.RenderTargetView)
                {
                    isShaderVisible = false;
                }

                Release();

                ID3D12Device device = D3D12Graphics.Device;
                Debug.Assert(device != null);

                DescriptorHeapDescription desc;
                desc.Flags = isShaderVisible
                    ? DescriptorHeapFlags.ShaderVisible
                    : DescriptorHeapFlags.None;
                desc.DescriptorCount = capacity;
                desc.Type = type;
                desc.NodeMask = 0;

                if (!D3D12Helpers.DxCall(device.CreateDescriptorHeap(desc, out heap)))
                {
                    return false;
                }

                freeHandles = new uint[capacity];
                this.capacity = capacity;
                size = 0;

                for (uint i = 0; i < capacity; i++)
                {
                    freeHandles[i] = i;
                }

                for (int i = 0; i < D3D12Graphics.FrameBufferCount; i++)
                {
                    Debug.Assert(deferredFreeIndices[i].Count == 0);
                }

                descriptorSize = (uint)device.GetDescriptorHandleIncrementSize(type);
                cpuStart = heap.GetCPUDescriptorHandleForHeapStart();
                gpuStart = isShaderVisible ?
                    heap.GetGPUDescriptorHandleForHeapStart() :
                    GpuDescriptorHandle.Default;

                return true;
            }
        }
        public void ProcessDeferredFree(int frameIdx)
        {
            lock (mutex)
            {
                Debug.Assert(frameIdx < D3D12Graphics.FrameBufferCount);

                var indices = deferredFreeIndices[frameIdx];
                if (indices.Count <= 0)
                {
                    return;
                }

                foreach (var index in indices)
                {
                    freeHandles[size--] = index;
                }
                indices.Clear();
            }
        }

        public D3D12DescriptorHandle Allocate()
        {
            lock (mutex)
            {
                Debug.Assert(heap != null);
                Debug.Assert(size < capacity);

                uint index = freeHandles[size];
                uint offset = index * descriptorSize;
                size++;

                D3D12DescriptorHandle handle = new();
                handle.Cpu.Ptr = cpuStart.Ptr + offset;
                if (IsShaderVisible)
                {
                    handle.Gpu.Ptr = gpuStart.Ptr + offset;
                }

                handle.Index = index;
                handle.Container = this;
                return handle;
            }
        }
        public void Free(ref D3D12DescriptorHandle handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            lock (mutex)
            {
                Debug.Assert(heap != null && size != 0);
                Debug.Assert(handle.Container == this);
                Debug.Assert(handle.Cpu.Ptr >= cpuStart.Ptr);
                Debug.Assert((int)(handle.Cpu.Ptr - cpuStart.Ptr) % descriptorSize == 0);
                Debug.Assert(handle.Index < capacity);
                var index = (uint)((handle.Cpu.Ptr - cpuStart.Ptr) / descriptorSize);
                Debug.Assert(handle.Index == index);

                int frameIdx = D3D12Graphics.CurrentFrameIndex;
                deferredFreeIndices[frameIdx].Add(index);
                D3D12Graphics.SetDeferredReleasesFlag();
                handle = new();
            }
        }
    }
}
