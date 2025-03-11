using Direct3D12.Helpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// A class that represents a constant buffer.
    /// </summary>
    unsafe class ConstantBuffer : IDisposable
    {
        private readonly object mutex = new();
        private readonly D3D12Buffer buffer;
        private byte* cpuAddress;
        private uint cpuOffset;

        public ID3D12Resource Buffer => buffer.Buffer;
        public ulong GpuAddress => buffer.GpuAddress;
        public uint Size => buffer.Size;
        public uint CpuOffset => cpuOffset;

        public ConstantBuffer(D3D12BufferInitInfo info)
        {
            buffer = new(info, true);

            D3D12Helpers.NameD3D12Object(buffer.Buffer, buffer.Size, "Constant Buffer - size");

            fixed (byte** cpuAddress = &this.cpuAddress)
            {
                D3D12Helpers.DxCall(buffer.Buffer.Map(0, cpuAddress));
                Debug.Assert(this.cpuAddress != null);
            }
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
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                buffer.Dispose();
                cpuAddress = null;
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
            T* p = Allocate<T>(1);
            // TODO: handle the case when cbuffer is full.
            BuffersHelper.Write(p, data);

            return GetGpuAddress(p);
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
            T* p = Allocate<T>(array.Length);
            // TODO: handle the case when cbuffer is full.
            BuffersHelper.WriteArray(p, array);

            return GetGpuAddress(p);
        }
        /// <summary>
        /// Allocates memory in the constant buffer.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <returns>Returns the CPU address</returns>
        private T* Allocate<T>(int arraySize) where T : unmanaged
        {
            lock (mutex)
            {
                uint size = (uint)(Marshal.SizeOf<T>() * arraySize);
                uint alignedSize = (uint)D3D12Helpers.AlignSizeForConstantBuffer(size);
                Debug.Assert(cpuOffset + alignedSize <= buffer.Size);
                if (cpuOffset + alignedSize <= buffer.Size)
                {
                    byte* address = cpuAddress + cpuOffset;
                    cpuOffset += alignedSize;
                    return (T*)address;
                }

                return null;
            }
        }
        /// <summary>
        /// Gets the GPU address of the CPU allocation.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="allocation">CPU allocation</param>
        /// <returns>Returns the GPU address</returns>
        private ulong GetGpuAddress<T>(T* allocation) where T : unmanaged
        {
            lock (mutex)
            {
                Debug.Assert(cpuAddress != null);
                if (cpuAddress == null)
                {
                    return 0;
                }

                byte* address = (byte*)allocation;

                Debug.Assert(address <= cpuAddress + cpuOffset);
                Debug.Assert(address >= cpuAddress);
                ulong offset = (ulong)(address - cpuAddress);
                return buffer.GpuAddress + offset;
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
