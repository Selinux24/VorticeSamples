using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Utilities
{
    /// <summary>
    /// Blob stream reader.
    /// </summary>
    public class BlobStreamReader
    {
        /// <summary>
        /// Start buffer pointer
        /// </summary>
        private readonly IntPtr buffer;
        /// <summary>
        /// Current buffer pointer
        /// </summary>
        private IntPtr position;

        /// <summary>
        /// Buffer start pointer
        /// </summary>
        public IntPtr Start => buffer;
        /// <summary>
        /// Current position
        /// </summary>
        public IntPtr Position => position;
        /// <summary>
        /// Current offset
        /// </summary>
        public int Offset => (int)(position - buffer);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer</param>
        public BlobStreamReader(IntPtr buffer)
        {
            Debug.Assert(buffer != IntPtr.Zero);
            this.buffer = buffer;
            position = buffer;
        }

        /// <summary>
        /// This method is intended to read primitive types (e.g. int, float, bool)
        /// </summary>
        /// <typeparam name="T">Primitive type</typeparam>
        /// <returns>Returns the value</returns>
        public T Read<T>() where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            T value = Marshal.PtrToStructure<T>(position);
            position += size;
            return value;
        }
        /// <summary>
        /// Reads 'count' elements of type T and returns them as an array.
        /// </summary>
        /// <typeparam name="T">Primitive type</typeparam>
        /// <param name="count">Array size</param>
        public T[] Read<T>(int count) where T : unmanaged
        {
            T[] values = new T[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Read<T>();
            }
            return values;
        }
        /// <summary>
        /// Reads 'length' bytes and returns them as a byte array.
        /// </summary>
        /// <param name="length">Buffer length</param>
        /// <returns>Byte array containing the read bytes</returns>
        public byte[] Read(int length)
        {
            byte[] buffer = new byte[length];
            Marshal.Copy(position, buffer, 0, length);
            position += length;
            return buffer;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(int offset)
        {
            position += offset;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(uint offset)
        {
            Skip((int)offset);
        }
    }
}
