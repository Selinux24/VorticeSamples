using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    static class D3D12Upload
    {
        #region Classes and Structs

        struct UploadFrame()
        {
            public ID3D12CommandAllocator CmdAllocator = null;
            public ID3D12GraphicsCommandList CmdList = null;
            public ID3D12Resource UploadBuffer = null;
            public bool IsReady = true;
            public IntPtr CpuAddress = IntPtr.Zero;
            public ulong FenceValue = 0;

            public void WaitAndReset()
            {
                Debug.Assert(uploadFence != null);
                if (uploadFence.CompletedValue < FenceValue)
                {
                    D3D12Helpers.DxCall(uploadFence.SetEventOnCompletion(FenceValue));
                }

                UploadBuffer?.Dispose();
                UploadBuffer = null;
                CpuAddress = IntPtr.Zero;
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
            readonly ID3D12GraphicsCommandList cmdList = null;
            ID3D12Resource uploadBuffer = null;
            IntPtr cpuAddress = IntPtr.Zero;
            uint frameIndex = uint.MaxValue;

            public ID3D12GraphicsCommandList CmdList { get => cmdList; }
            public ID3D12Resource UploadBuffer { get => uploadBuffer; }
            public IntPtr CpuAddress { get => cpuAddress; }

            public UploadContext(uint alignedSize)
            {
                Debug.Assert(uploadCmdQueue != null);

                lock (frameMutex)
                {
                    frameIndex = GetAvailableUploadFrame();
                    Debug.Assert(frameIndex != uint.MaxValue);
                    uploadFrames[frameIndex].IsReady = false;
                }

                uploadFrames[frameIndex].UploadBuffer = D3D12Helpers.CreateBuffer<byte>(null, alignedSize, true);
                D3D12Helpers.NameD3D12Object(uploadFrames[frameIndex].UploadBuffer, alignedSize, "Upload Buffer - size");

                IntPtr mapped = IntPtr.Zero;
                unsafe
                {
                    D3D12Helpers.DxCall(uploadFrames[frameIndex].UploadBuffer.Map(0, &mapped));
                }
                Debug.Assert(mapped != IntPtr.Zero);

                uploadFrames[frameIndex].CpuAddress = mapped;

                cmdList = uploadFrames[frameIndex].CmdList;
                uploadBuffer = uploadFrames[frameIndex].UploadBuffer;
                cpuAddress = uploadFrames[frameIndex].CpuAddress;
                Debug.Assert(cmdList != null && uploadBuffer != null && cpuAddress != IntPtr.Zero);

                uploadFrames[frameIndex].CmdAllocator.Reset();
                uploadFrames[frameIndex].CmdList.Reset(uploadFrames[frameIndex].CmdAllocator, null);
            }

            public void CopyData(List<SubresourceData> subresources, uint subresourceCount, PlacedSubresourceFootPrint[] layouts, uint[] numRows, ulong[] rowSizes)
            {
                for (int subresourceIdx = 0; subresourceIdx < subresourceCount; subresourceIdx++)
                {
                    var layout = layouts[subresourceIdx];
                    uint subresourceHeight = numRows[subresourceIdx];
                    uint subresourceDepth = layout.Footprint.Depth;
                    var subResource = subresources[subresourceIdx];

                    int destOffset = checked((int)layout.Offset);
                    uint destRowPitch = layout.Footprint.RowPitch;
                    uint destSlicePitch = layout.Footprint.RowPitch * subresourceHeight;

                    unsafe
                    {
                        for (uint depthIdx = 0; depthIdx < subresourceDepth; depthIdx++)
                        {
                            nint srcSliceBase = (nint)subResource.pData + (nint)(subResource.SlicePitch * depthIdx);
                            int dstSliceOffset = destOffset + checked((int)(destSlicePitch * depthIdx));

                            for (uint rowIdx = 0; rowIdx < subresourceHeight; rowIdx++)
                            {
                                nint srcRow = srcSliceBase + (nint)((uint)subResource.RowPitch * rowIdx);
                                int dstRowOffset = dstSliceOffset + checked((int)(destRowPitch * rowIdx));
                                ulong bytesPerRow = checked(rowSizes[subresourceIdx]);

                                Buffer.MemoryCopy(
                                    (void*)srcRow,
                                    (void*)(cpuAddress + dstRowOffset), bytesPerRow, bytesPerRow);
                            }
                        }
                    }
                }
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

                    uploadFrames[frameIndex].WaitAndReset();

                    cmdList = null;
                    uploadBuffer = null;
                    cpuAddress = IntPtr.Zero;
                    frameIndex = uint.MaxValue;
                }
            }
        }

        #endregion

        const uint UploadFrameCount = 4;
        static readonly UploadFrame[] uploadFrames = new UploadFrame[UploadFrameCount];
        static ID3D12CommandQueue uploadCmdQueue = null;
        static ID3D12Fence1 uploadFence = null;
        static ulong uploadFenceValue = 0;
        static readonly object frameMutex = new();
        static readonly object queueMutex = new();

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

            return true;
        }
        public static void Shutdown()
        {
            for (int i = 0; i < UploadFrameCount; i++)
            {
                uploadFrames[i].Release();
            }

            uploadCmdQueue?.Dispose();
            uploadCmdQueue = null;
            uploadFence?.Dispose();
            uploadFence = null;
            uploadFenceValue = 0;
        }

        /// <summary>
        /// Returns the index of the first available frame.
        /// </summary>
        /// <remarks>
        /// NOTE: frames should be locked before this function is called.
        /// </remarks>
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
    }
}
