using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
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
}
