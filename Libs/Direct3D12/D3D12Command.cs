using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12Command
    {
        class CommandFrame
        {
            public ID3D12CommandAllocator CmdAllocator;
            public ulong FenceValue;

            public void Wait(AutoResetEvent fenceEvent, ID3D12Fence1 fence)
            {
                Debug.Assert(fenceEvent != null && fence != null);

                // If the current fence value is still less than "fence_value"
                // then we know the GPU has not finished executing the command lists
                // since it has not reached the "_cmd_queue->Signal()" command
                if (fence.CompletedValue < FenceValue)
                {
                    // We have the fence create an event wich is singaled once the fence's current value equals "fence_value"
                    fence.SetEventOnCompletion(FenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle());

                    // Wait until the fence has triggered the event that its current value has reached "fence_value"
                    // indicating that command queue has finished executing.
                    fenceEvent.WaitOne();
                }
            }
            public void Release()
            {
                CmdAllocator.Release();
                FenceValue = 0;
            }
        }

        private readonly ID3D12CommandQueue cmdQueue;
        private readonly D3D12GraphicsCommandList cmdList;
        private readonly ID3D12Fence1 fence;
        private ulong fenceValue = 0;
        private AutoResetEvent fenceEvent;

        private readonly int frameBufferCount;
        private readonly CommandFrame[] cmdFrames;
        private int frameIndex = 0;

        public ID3D12CommandQueue CommandQueue { get => cmdQueue; }
        public D3D12GraphicsCommandList CommandList { get => cmdList; }
        public int FrameIndex { get => frameIndex; }

        public D3D12Command(CommandListType type)
        {
            frameBufferCount = D3D12Graphics.FrameBufferCount;
            var device = D3D12Graphics.Device;

            var description = new CommandQueueDescription(type, CommandQueuePriority.Normal, CommandQueueFlags.None, 0);

            if (!D3D12Helpers.DxCall(device.CreateCommandQueue(description, out cmdQueue)))
            {
                Release();
                return;
            }
            string cmdQueueName = type switch
            {
                CommandListType.Direct => "GFX Command Queue",
                CommandListType.Compute => "Compute Command Queue",
                _ => "Command Queue"
            };
            D3D12Helpers.NameD3D12Object(cmdQueue, cmdQueueName);

            cmdFrames = new CommandFrame[frameBufferCount];
            for (int i = 0; i < frameBufferCount; i++)
            {
                cmdFrames[i] = new CommandFrame();
            }

            for (int i = 0; i < frameBufferCount; i++)
            {
                var frame = cmdFrames[i];

                if (!D3D12Helpers.DxCall(device.CreateCommandAllocator(type, out frame.CmdAllocator)))
                {
                    Release();
                    return;
                }
                string cmdAllocatorName = type switch
                {
                    CommandListType.Direct => "GFX Command Allocator",
                    CommandListType.Compute => "Compute Command Allocator",
                    _ => "Command Allocator"
                };
                D3D12Helpers.NameD3D12Object(frame.CmdAllocator, i, cmdAllocatorName);
            }

            if (!D3D12Helpers.DxCall(device.CreateCommandList(0, type, cmdFrames[0].CmdAllocator, null, out cmdList)))
            {
                Release();
            }
            cmdList.Close();
            string cmdListName = type switch
            {
                CommandListType.Direct => "GFX Command List",
                CommandListType.Compute => "Compute Command List",
                _ => "Command List"
            };
            D3D12Helpers.NameD3D12Object(cmdList, cmdListName);

            if (!D3D12Helpers.DxCall(device.CreateFence(0, FenceFlags.None, out fence)))
            {
                Release();
            }
            D3D12Helpers.NameD3D12Object(fence, "D3D12 Fence");

            fenceEvent = new AutoResetEvent(false);
            Debug.Assert(fenceEvent != null);
        }

        public void BeginFrame()
        {
            CommandFrame frame = cmdFrames[frameIndex];
            frame.Wait(fenceEvent, fence);
            frame.CmdAllocator.Reset();
            cmdList.Reset(frame.CmdAllocator, null);
        }
        public void EndFrame(D3D12Surface surface)
        {
            cmdList.Close();
            cmdQueue.ExecuteCommandLists([cmdList]);

            surface.Present();

            ulong value = fenceValue;
            ++value;
            CommandFrame frame = cmdFrames[frameIndex];
            frame.FenceValue = value;
            cmdQueue.Signal(fence, value);

            frameIndex = (frameIndex + 1) % frameBufferCount;
        }
        public void Flush()
        {
            for (int i = 0; i < frameBufferCount; i++)
            {
                cmdFrames[i].Wait(fenceEvent, fence);
            }
            frameIndex = 0;
        }
        public void Release()
        {
            Flush();
            fence.Release();
            fenceValue = 0;

            fenceEvent.Close();
            fenceEvent.Dispose();
            fenceEvent = null;

            cmdQueue.Release();
            cmdList.Release();

            for (int i = 0; i < frameBufferCount; i++)
            {
                cmdFrames[i].Release();
            }
        }
    }
}