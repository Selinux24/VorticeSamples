using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class DescriptorHeap
    {
        private readonly D3D12Graphics graphics;
        private readonly DescriptorHeapType type;
        private readonly Mutex mutex;
        private readonly List<int>[] deferredFreeIndices;

        private ID3D12DescriptorHeap heap;
        private CpuDescriptorHandle cpuStart;
        private GpuDescriptorHandle gpuStart;
        private IntPtr[] freeHandles;
        private int capacity = 0;
        private int size = 0;
        private int descriptorSize;

        public DescriptorHeapType DescriptorType { get => type; }
        public CpuDescriptorHandle CpuStart { get => cpuStart; }
        public GpuDescriptorHandle GpuStart { get => gpuStart; }
        public ID3D12DescriptorHeap Heap { get => heap; }
        public int Capacity { get => capacity; }
        public int Size { get => size; }
        public int DescriptorSize { get => descriptorSize; }
        public bool IsShaderVisible { get => gpuStart.Ptr != 0; }

        public DescriptorHeap(D3D12Graphics graphics, DescriptorHeapType type)
        {
            this.graphics = graphics;
            this.type = type;

            mutex = new();

            deferredFreeIndices = new List<int>[D3D12Graphics.FrameBufferCount];
            for (int i = 0; i < D3D12Graphics.FrameBufferCount; i++)
            {
                deferredFreeIndices[i] = [];
            }
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

                ID3D12Device device = graphics.Device;
                Debug.Assert(device != null);

                DescriptorHeapDescription desc;
                desc.Flags = isShaderVisible
                    ? DescriptorHeapFlags.ShaderVisible
                    : DescriptorHeapFlags.None;
                desc.DescriptorCount = capacity;
                desc.Type = type;
                desc.NodeMask = 0;

                if (!device.CreateDescriptorHeap(desc, out heap).Success)
                {
                    return false;
                }

                freeHandles = new nint[capacity];
                this.capacity = capacity;
                size = 0;

                for (int i = 0; i < capacity; i++)
                {
                    freeHandles[i] = i;
                }

                for (int i = 0; i < D3D12Graphics.FrameBufferCount; i++)
                {
                    Debug.Assert(deferredFreeIndices[i].Count == 0);
                }

                descriptorSize = device.GetDescriptorHandleIncrementSize(type);
                cpuStart = heap.GetCPUDescriptorHandleForHeapStart();
                gpuStart = isShaderVisible ?
                    heap.GetGPUDescriptorHandleForHeapStart() :
                    GpuDescriptorHandle.Default;

                return true;
            }
        }
        public void Release()
        {
            Debug.Assert(size == 0);
            graphics.DeferredRelease(heap);
        }
        public void ProcessDeferredFree(int frameIdx)
        {
            lock (mutex)
            {
                Debug.Assert(frameIdx < D3D12Graphics.FrameBufferCount);

                var indices = deferredFreeIndices[frameIdx];
                if (indices.Count != 0)
                {
                    foreach (var index in indices)
                    {
                        --size;
                        freeHandles[size] = index;
                    }
                    indices.Clear();
                }
            }
        }

        public DescriptorHandle Allocate()
        {
            lock (mutex)
            {
                Debug.Assert(heap != null);
                Debug.Assert(size < capacity);

                var index = freeHandles[size];
                nuint offset = (nuint)descriptorSize;
                ++size;

                DescriptorHandle handle = new();
                handle.Cpu.Ptr = cpuStart.Ptr + offset;
                if (IsShaderVisible)
                {
                    handle.Gpu.Ptr = gpuStart.Ptr + offset;
                }

                handle.Container = this;
                handle.Index = index;
                return handle;
            }
        }

        public void Free(ref DescriptorHandle handle)
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
                int index = (int)(handle.Cpu.Ptr - cpuStart.Ptr) / descriptorSize;
                Debug.Assert(handle.Index == index);

                int frameIdx = graphics.CurrentFrameIndex();
                deferredFreeIndices[frameIdx].Add(index);
                graphics.SetDeferredReleasesFlag();
                handle = new();
            }
        }
    }
}
