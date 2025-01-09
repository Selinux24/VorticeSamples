using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    unsafe static class D3D12Upload
    {
        struct UploadFrame()
        {
            public ID3D12CommandAllocator CmdAllocator = null;
            public ID3D12GraphicsCommandList CmdList = null;
            public ID3D12Resource UploadBuffer = null;
            public bool IsReady = true;
            public void* CpuAddress = null;
            public ulong FenceValue = 0;

            public void WaitAndReset()
            {
                Debug.Assert(uploadFence != null && fenceEvent != null);
                if (uploadFence.CompletedValue < FenceValue)
                {
                    D3D12Helpers.DxCall(uploadFence.SetEventOnCompletion(FenceValue, fenceEvent));
                    fenceEvent.WaitOne();
                }

                UploadBuffer?.Dispose();
                UploadBuffer = null;
                CpuAddress = null;
                IsReady = true;
            }
            public void Release()
            {
                WaitAndReset();
                CmdAllocator.Dispose();
                CmdList.Dispose();
            }
        }
        public class UploadContext
        {
            private readonly ID3D12GraphicsCommandList cmdList = null;
            private ID3D12Resource uploadBuffer = null;
            private void* cpuAddress = null;
            private uint frameIndex = uint.MaxValue;

            public ID3D12GraphicsCommandList CmdList { get => cmdList; }
            public ID3D12Resource UploadBuffer { get => uploadBuffer; }
            public void* CpuAddress { get => cpuAddress; }

            public UploadContext(uint alignedSize)
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

                uploadFrames[frameIndex].UploadBuffer = D3D12Helpers.CreateBuffer<byte>(null, alignedSize, true);
                D3D12Helpers.NameD3D12Object(uploadFrames[frameIndex].UploadBuffer, (int)alignedSize, "Upload Buffer - size");

                fixed (void* cpuAddressBegin = &uploadFrames[frameIndex].CpuAddress)
                {
                    D3D12Helpers.DxCall(uploadFrames[frameIndex].UploadBuffer.Map(0, cpuAddressBegin));
                    Debug.Assert(uploadFrames[frameIndex].CpuAddress != null);
                }

                cmdList = uploadFrames[frameIndex].CmdList;
                uploadBuffer = uploadFrames[frameIndex].UploadBuffer;
                cpuAddress = uploadFrames[frameIndex].CpuAddress;
                Debug.Assert(cmdList != null && uploadBuffer != null && cpuAddress != null);

                uploadFrames[frameIndex].CmdAllocator.Reset();
                uploadFrames[frameIndex].CmdList.Reset(uploadFrames[frameIndex].CmdAllocator, null);
            }

            public void EndUpload()
            {
                Debug.Assert(frameIndex != uint.MaxValue);
                var cmdList = uploadFrames[frameIndex].CmdList;
                cmdList.Close();

                lock (queueMutex)
                {
                    ID3D12GraphicsCommandList[] cmdLists = [cmdList];
                    ID3D12CommandQueue cmdQueue = uploadCmdQueue;
                    cmdQueue.ExecuteCommandLists(cmdLists);

                    uploadFrames[frameIndex].FenceValue = ++uploadFenceValue;
                    D3D12Helpers.DxCall(cmdQueue.Signal(uploadFence, uploadFrames[frameIndex].FenceValue));

                    // Wait for copy queue to finish. Then release the upload buffer.
                    uploadFrames[frameIndex].WaitAndReset();

                    // This instance of upload context is now expired. Make sure we don't use it again.
                    cmdList = null;
                    uploadBuffer = null;
                    cpuAddress = null;
                    frameIndex = uint.MaxValue;
                }
            }
        }

        private const uint UploadFrameCount = 4;
        private static readonly UploadFrame[] uploadFrames = new UploadFrame[UploadFrameCount];
        private static ID3D12CommandQueue uploadCmdQueue = null;
        private static ID3D12Fence1 uploadFence = null;
        private static ulong uploadFenceValue = 0;
        private static AutoResetEvent fenceEvent = null;
        private static readonly object frameMutex = new();
        private static readonly object queueMutex = new();

        public static bool Initialize()
        {
            var device = D3D12Graphics.Device;
            Debug.Assert(device != null && uploadCmdQueue == null);

            for (int i = 0; i < UploadFrameCount; i++)
            {
                UploadFrame frame = new();

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

                uploadFrames[i] = frame;
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

        /// <summary>
        /// Returns the index of the first available frame.
        /// </summary>
        /// <remarks>
        /// NOTE: frames should be locked before this function is called.
        /// </remarks>
        private static uint GetAvailableUploadFrame()
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
        private static bool InitFailed()
        {
            Shutdown();
            return false;
        }
    }
}
