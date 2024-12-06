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
        private readonly ID3D12GraphicsCommandList6 cmdList;
        private readonly ID3D12Fence1 fence;
        private ulong fenceValue = 0;
        private AutoResetEvent fenceEvent;

        private readonly int frameBufferCount;
        private readonly CommandFrame[] cmdFrames;
        private int frameIndex = 0;

        public ID3D12CommandQueue CommandQueue { get => cmdQueue; }
        public ID3D12GraphicsCommandList6 CommandList { get => cmdList; }
        public int FrameIndex { get => frameIndex; }

        public D3D12Command(D3D12Graphics graphics, CommandListType type)
        {
            frameBufferCount = graphics.FrameBufferCount;
            var device = graphics.Device;

            var description = new CommandQueueDescription(type, CommandQueuePriority.Normal, CommandQueueFlags.None, 0);

            if (!device.CreateCommandQueue(description, out cmdQueue).Success)
            {
                Release();
                return;
            }
            cmdQueue.Name = type switch
            {
                CommandListType.Direct => "GFX Command Queue",
                CommandListType.Compute => "Compute Command Queue",
                _ => "Command Queue"
            };

            cmdFrames = new CommandFrame[frameBufferCount];
            for (int i = 0; i < frameBufferCount; i++)
            {
                cmdFrames[i] = new CommandFrame();
            }

            for (int i = 0; i < frameBufferCount; i++)
            {
                var frame = cmdFrames[i];

                if (!device.CreateCommandAllocator(type, out frame.CmdAllocator).Success)
                {
                    Release();
                    return;
                }
                frame.CmdAllocator.Name = type switch
                {
                    CommandListType.Direct => $"GFX Command Allocator [{i}]",
                    CommandListType.Compute => $"Compute Command Allocator [{i}]",
                    _ => $"Command Allocator [{i}]"
                };
            }

            if (!device.CreateCommandList(0, type, cmdFrames[0].CmdAllocator, null, out cmdList).Success)
            {
                Release();
            }
            cmdList.Close();
            cmdList.Name = type switch
            {
                CommandListType.Direct => "GFX Command List",
                CommandListType.Compute => "Compute Command List",
                _ => "Command List"
            };

            if (!device.CreateFence(0, FenceFlags.None, out fence).Success)
            {
                Release();
            }
            fence.Name = "D3D12 Fence";

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
        public void EndFrame()
        {
            cmdList.Close();
            cmdQueue.ExecuteCommandLists([cmdList]);

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