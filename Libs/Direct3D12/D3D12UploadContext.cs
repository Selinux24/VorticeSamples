using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    unsafe class D3D12UploadContext
    {
        struct UploadFrame()
        {
            public ID3D12CommandAllocator CmdAllocator = null;
            public ID3D12GraphicsCommandList CmdList = null;
            public ID3D12Resource UploadBuffer = null;
            public void* CpuAddress = null;
            public ulong FenceValue = 0;

            public bool IsReady { get; set; } = true;

            public void WaitAndReset()
            {
                Debug.Assert(uploadFence != null && fenceEvent != null);
                if (uploadFence.CompletedValue < FenceValue)
                {
                    D3D12Helpers.DxCall(uploadFence.SetEventOnCompletion(FenceValue, fenceEvent));
                    fenceEvent.WaitOne();
                }

                UploadBuffer.Dispose();
                CpuAddress = null;
                IsReady = true;
            }

            public void Release()
            {
                WaitAndReset();
                CmdAllocator.Dispose();
                CmdList.Dispose();
            }
        };

        const uint UploadFrameCount = 4;
        static readonly UploadFrame[] uploadFrames = new UploadFrame[UploadFrameCount];
        static ID3D12CommandQueue uploadCmdQueue = null;
        static ID3D12Fence1 uploadFence = null;
        static ulong uploadFenceValue = 0;
        static AutoResetEvent fenceEvent = null;
        static readonly object frameMutex = new();
        static readonly object queueMutex = new();

        // NOTE: frames should be locked before this function is called.
        static uint GetAvailableUploadFrame()
        {
            uint index = uint.MaxValue;
            const uint count = UploadFrameCount;
            for (uint i = 0; i < count; ++i)
            {
                if (uploadFrames[i].IsReady)
                {
                    index = i;
                    break;
                }
            }
            // None of the frames were done uploading. We're the only thread here, so
            // we can iterate through frames until we find one that is ready.
            if (index == uint.MaxValue)
            {
                index = 0;
                while (!uploadFrames[index].IsReady)
                {
                    index = (index + 1) % count;
                    Thread.Yield();
                }
            }
            return index;
        }
        static bool InitFailed()
        {
            Shutdown();
            return false;
        }

        private readonly ID3D12GraphicsCommandList cmdList = null;
        private readonly ID3D12Resource uploadBuffer = null;
        private readonly void* cpuAddress = null;
        private readonly uint frameIndex = uint.MaxValue;

        public ID3D12GraphicsCommandList CmdList { get => cmdList; }
        public ID3D12Resource UploadBuffer { get => uploadBuffer; }
        public void* CpuAddress { get => cpuAddress; }

        public static bool Initialize()
        {
            var device = D3D12Graphics.Device;
            Debug.Assert(device != null && uploadCmdQueue == null);

            for (int i = 0; i < UploadFrameCount; i++)
            {
                var frame = uploadFrames[i];
                if (!D3D12Helpers.DxCall(device.CreateCommandAllocator(CommandListType.Copy, out frame.CmdAllocator)))
                {
                    return InitFailed();
                }

                if (!D3D12Helpers.DxCall(device.CreateCommandList(0, CommandListType.Copy, frame.CmdAllocator, null, out frame.CmdList)))
                {
                    return InitFailed();
                }

                frame.CmdList.Close();

                D3D12Helpers.NameD3D12Object(frame.CmdAllocator, i, "Upload Command Allocator");
                D3D12Helpers.NameD3D12Object(frame.CmdList, i, "Upload Command List");
            }

            CommandQueueDescription desc = new()
            {
                Flags = CommandQueueFlags.None,
                NodeMask = 0,
                Priority = (int)CommandQueuePriority.Normal,
                Type = CommandListType.Copy
            };

            if (!D3D12Helpers.DxCall(device.CreateCommandQueue(desc, out uploadCmdQueue)))
            {
                return InitFailed();
            }
            D3D12Helpers.NameD3D12Object(uploadCmdQueue, "Upload Copy Queue");

            if (!D3D12Helpers.DxCall(device.CreateFence(0, FenceFlags.None, out uploadFence)))
            {
                return InitFailed();
            }
            D3D12Helpers.NameD3D12Object(uploadFence, "Upload Fence");

            fenceEvent = new AutoResetEvent(false);
            Debug.Assert(fenceEvent != null);
            if (fenceEvent == null)
            {
                return InitFailed();
            }

            return true;
        }
        public static void Shutdown()
        {
            for (int i = 0; i < UploadFrameCount; i++)
            {
                uploadFrames[i].Release();
            }

            if (fenceEvent != null)
            {
                fenceEvent.Dispose();
                fenceEvent = null;
            }

            uploadCmdQueue.Dispose();
            uploadFence.Dispose();
            uploadFenceValue = 0;
        }

        public D3D12UploadContext(uint alignedSize)
        {
            Debug.Assert(uploadCmdQueue != null);

            // We don't want to lock this function for longer than necessary. So, we scope this lock.
            lock (frameMutex)
            {
                frameIndex = GetAvailableUploadFrame();
                Debug.Assert(frameIndex != uint.MaxValue);
                // Before unlocking, we prevent other threads from picking
                // this frame by making is_ready return false.
                uploadFrames[frameIndex].IsReady = false;
            }

            UploadFrame frame = uploadFrames[frameIndex];
            frame.UploadBuffer = D3D12Helpers.CreateBuffer<byte>(null, alignedSize, true);
            D3D12Helpers.NameD3D12Object(frame.UploadBuffer, (int)alignedSize, "Upload Buffer - size");

            Range range = new();
            D3D12Helpers.DxCall(frame.UploadBuffer.Map(0, range, frame.CpuAddress));
            Debug.Assert(frame.CpuAddress != null);

            cmdList = frame.CmdList;
            uploadBuffer = frame.UploadBuffer;
            cpuAddress = frame.CpuAddress;
            Debug.Assert(cmdList != null && uploadBuffer != null && cpuAddress != null);

            frame.CmdAllocator.Reset();
            frame.CmdList.Reset(frame.CmdAllocator, null);
        }
        ~D3D12UploadContext()
        {
            Debug.Assert(frameIndex == uint.MaxValue);
        }

        public void EndUpload()
        {
            Debug.Assert(frameIndex != uint.MaxValue);
            var frame = uploadFrames[frameIndex];
            var cmdList = frame.CmdList;
            cmdList.Close();

            lock (queueMutex)
            {
                ID3D12GraphicsCommandList[] cmdLists = [cmdList];
                ID3D12CommandQueue cmdQueue = uploadCmdQueue;
                cmdQueue.ExecuteCommandLists(cmdLists);

                frame.FenceValue = ++uploadFenceValue;
                D3D12Helpers.DxCall(cmdQueue.Signal(uploadFence, frame.FenceValue));

                // Wait for copy queue to finish. Then release the upload buffer.
                frame.WaitAndReset();
            }
        }
    }
}
