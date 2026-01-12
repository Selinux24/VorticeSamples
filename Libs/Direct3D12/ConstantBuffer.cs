using Direct3D12.Helpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// A class that represents a constant buffer.
    /// </summary>
    class ConstantBuffer : IDisposable
    {
        readonly Lock mutex = new();
        readonly D3D12Buffer buffer;
        IntPtr cpuAddress;
        uint cpuOffset;

        public ID3D12Resource Buffer => buffer.Buffer;
        public IntPtr CpuAddress => cpuAddress;
        public ulong GpuAddress => buffer.GpuAddress;
        public uint Size => buffer.Size;
        public uint CpuOffset => cpuOffset;

        public ConstantBuffer(D3D12BufferInitInfo info)
        {
            buffer = new(info, true);

            D3D12Helpers.NameD3D12Object(buffer.Buffer, buffer.Size, "Constant Buffer - size");

            D3D12Helpers.DxCall(buffer.Buffer.Map(0, out cpuAddress));
            Debug.Assert(cpuAddress != IntPtr.Zero);
        }
        ~ConstantBuffer()
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
                buffer.Dispose();
                cpuAddress = IntPtr.Zero;
                cpuOffset = 0;
            }
        }

        /// <summary>
        /// Write data to the constant buffer. This is a slow operation.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="data">Data</param>
        /// <returns>Returns the GPU address</returns>
        public ulong Write<T>(T data) where T : unmanaged
        {
            // NOTE: be careful not to read from this buffer. Reads are really really slow.
            var p = Allocate<T>(1);
            // TODO: handle the case when cbuffer is full.
            BuffersHelper.WriteUnaligned(data, p);

            return GetGpuAddress<T>(p);
        }
        /// <summary>
        /// Write data to the constant buffer. This is a slow operation.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="array">Array</param>
        /// <returns>Returns the GPU address</returns>
        public ulong Write<T>(T[] array) where T : unmanaged
        {
            // NOTE: be careful not to read from this buffer. Reads are really really slow.
            var p = Allocate<T>(array.Length);
            // TODO: handle the case when cbuffer is full.
            BuffersHelper.WriteUnaligned(array, p);

            return GetGpuAddress<T>(p);
        }
        /// <summary>
        /// Allocates memory in the constant buffer.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <returns>Returns the CPU address</returns>
        IntPtr Allocate<T>(int arraySize) where T : unmanaged
        {
            lock (mutex)
            {
                uint size = (uint)(Marshal.SizeOf<T>() * arraySize);
                uint alignedSize = (uint)D3D12Helpers.AlignSizeForConstantBuffer(size);
                Debug.Assert(cpuOffset + alignedSize <= buffer.Size);
                if (cpuOffset + alignedSize <= buffer.Size)
                {
                    IntPtr address = cpuAddress + (IntPtr)cpuOffset;
                    cpuOffset += alignedSize;
                    return address;
                }

                return IntPtr.Zero;
            }
        }
        /// <summary>
        /// Gets the GPU address of the CPU allocation.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="allocation">CPU allocation</param>
        /// <returns>Returns the GPU address</returns>
        ulong GetGpuAddress<T>(IntPtr allocation) where T : unmanaged
        {
            lock (mutex)
            {
                Debug.Assert(cpuAddress != IntPtr.Zero);
                if (cpuAddress == IntPtr.Zero)
                {
                    return 0;
                }

                IntPtr address = allocation;

                Debug.Assert(address <= (cpuAddress + cpuOffset));
                Debug.Assert(address >= cpuAddress);
                IntPtr offset = address - cpuAddress;
                return buffer.GpuAddress + (ulong)offset;
            }
        }

        /// <summary>
        /// Clears the constant buffer.
        /// </summary>
        /// <remarks>
        /// Note that this does not actually clear the buffer, it just resets the offset.
        /// </remarks>
        public void Clear()
        {
            cpuOffset = 0;
        }

        /// <summary>
        /// Gets the default initialization info.
        /// </summary>
        /// <param name="size">Size</param>
        public static D3D12BufferInitInfo GetDefaultInitInfo(uint size)
        {
            Debug.Assert(size > 0);

            return new()
            {
                Size = size,
                Alignment = D3D12.ConstantBufferDataPlacementAlignment
            };
        }
    }
}
