using Direct3D12.Helpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class CommandBuffer : IDisposable
    {
        D3D12Buffer cmdBuffer = new();
        IntPtr cpuAddress = IntPtr.Zero;
        uint bufferSize = 0;
        DrawIndexedIndirectCommand[] commands = [];

        public ID3D12Resource Buffer { get { return cmdBuffer.Buffer; } }
        public uint Size { get { return bufferSize; } }
        public DrawIndexedIndirectCommand[] Commands { get { return commands; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandBuffer()
        {

        }
        ~CommandBuffer()
        {
            Dispose(false);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                cmdBuffer.Dispose();
                cpuAddress = IntPtr.Zero;
            }
        }

        public void UploadGPassCommands() { UploadCommands(true); }
        public void UploadDepthCommands() { UploadCommands(false); }
        void UploadCommands(bool isGPass)
        {
            if (bufferSize == 0) return;

            IntPtr dst = isGPass ? cpuAddress : cpuAddress + (IntPtr)bufferSize;
            BuffersHelper.WriteUnaligned(commands, dst);
        }

        public void Resize(uint itemsCount)
        {
            Debug.Assert(itemsCount > 0);
            Array.Resize(ref commands, (int)itemsCount);
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i] = new DrawIndexedIndirectCommand();
            }
            bufferSize = itemsCount * (uint)Marshal.SizeOf<DrawIndexedIndirectCommand>();

            // Create a buffer twice the size of the items count for both gpass and depth pass commands.
            // The first half will be used for gpass commands and the second half for depth pass commands.
            uint totalSize = bufferSize * 2;

            if (cmdBuffer.Size < (uint)D3D12Helpers.AlignSizeForConstantBuffer(totalSize))
            {
                cmdBuffer = new(ConstantBuffer.GetDefaultInitInfo(totalSize), true);
                D3D12Helpers.NameD3D12Object(cmdBuffer.Buffer, D3D12Graphics.CurrentFrameIndex, "Indirect Command Buffer");

                D3D12Helpers.DxCall(cmdBuffer.Buffer.Map(0, out cpuAddress));
                Debug.Assert(cpuAddress != IntPtr.Zero);
            }
        }
    }
}
