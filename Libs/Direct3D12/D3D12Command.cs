using System;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12Command
    {
        ID3D12CommandQueue cmdQueue;
        ID3D12GraphicsCommandList6 cmdList;
        ID3D12Fence1 fence;
        ulong fenceValue = 0;
        CommandFrame[] cmdFrames;
        IntPtr fenceEvent;
        int frameIndex = 0;

        public D3D12Command(ID3D12Device8 device, CommandListType type)
        {
            var description = new CommandQueueDescription(
                type,
                CommandQueuePriority.Normal,
                CommandQueueFlags.None,
                0);

            if (!device.CreateCommandQueue(description, out var commandQueue).Success)
            {
                Release();
                return;
            }
            commandQueue.Name = type switch
            {
                CommandListType.Direct => "GFX Command Queue",
                CommandListType.Compute => "Compute Command Queue",
                _ => "Command Queue"
            };

            cmdFrames = new CommandFrame[D3D12Graphics.FrameBufferCount];
            for (int i = 0; i < D3D12Graphics.FrameBufferCount; ++i)
            {
                var frame = cmdFrames[i];
                if (!device.CreateCommandAllocator(type, out frame.CmdAllocator).Success)
                {
                    Release();
                }
                frame.CmdAllocator.Name = type switch
                {
                    CommandListType.Direct => "GFX Command Allocator",
                    CommandListType.Compute => "Compute Command Allocator",
                    _ => "Command Allocator"
                };
            }

            if (!device.CreateCommandList<ID3D12GraphicsCommandList6>(0, type, cmdFrames[0].CmdAllocator, null, out var cmdList).Success)
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

            if (!device.CreateFence(0, FenceFlags.None, out var fence).Success)
            {
                Release();
            }
            fence.Name = "D3D12 Fence";

            
        }

        public void BeginFrame()
        {
            CommandFrame frame = cmdFrames[frameIndex];
            frame.Wait(fenceEvent, fence);
            frame.CmdAllocator.Reset();
            cmdList.Reset(frame.CmdAllocator, null);
        }

        public void Release()
        {
            
        }
    }

    class CommandFrame
    {
        public ID3D12CommandAllocator CmdAllocator;
        public ulong FenceValue;

        public void Wait(IntPtr fenceEvent, ID3D12Fence1 fence)
        {
            Debug.Assert(fence != null && fenceEvent != IntPtr.Zero);

            // If the current fence value is still less than "fence_value"
            // then we know the GPU has not finished executing the command lists
            // since it has not reached the "_cmd_queue->Signal()" command
            if (fence.CompletedValue < FenceValue)
            {
                // We have the fence create an event wich is singaled one the fence's current value equals "fence_value"
                fence.SetEventOnCompletion(FenceValue, fenceEvent);

                // Wait until the fence has triggered the event that its current value has reached "fence_value"
                // indicating that command queue has finished executing.
                //WaitForSingleObject(fenceEvent, INFINITE);
            }
        }

        public void Release()
        {
            CmdAllocator.Release();
        }
    }
}