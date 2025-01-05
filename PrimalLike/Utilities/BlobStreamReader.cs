using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PrimalLike.Utilities
{
    /// <summary>
    /// Blob stream reader.
    /// </summary>
    public class BlobStreamReader
    {
        private readonly IntPtr _buffer;
        private int _position;

        /// <summary>
        /// Buffer start pointer
        /// </summary>
        public IntPtr BufferStart => _buffer;
        /// <summary>
        /// Current position
        /// </summary>
        public int Position => _position;
        /// <summary>
        /// Current offset
        /// </summary>
        public int Offset => _position;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer</param>
        public BlobStreamReader(IntPtr buffer)
        {
            Debug.Assert(buffer != IntPtr.Zero);
            _buffer = buffer;
            _position = 0;
        }

        /// <summary>
        /// This method is intended to read primitive types (e.g. int, float, bool)
        /// </summary>
        /// <typeparam name="T">Primitive type</typeparam>
        /// <returns>Returns the value</returns>
        public T Read<T>() where T : struct
        {
            int size = Marshal.SizeOf<T>();
            T value = Marshal.PtrToStructure<T>(_buffer + _position);
            _position += size;
            return value;
        }
        /// <summary>
        /// Reads 'length' bytes and returns them as a byte array.
        /// </summary>
        /// <param name="length">Buffer length</param>
        /// <returns>Byte array containing the read bytes</returns>
        public byte[] Read(int length)
        {
            byte[] buffer = new byte[length];
            Marshal.Copy(_buffer + _position, buffer, 0, length);
            _position += length;
            return buffer;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(int offset)
        {
            _position += offset;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(uint offset)
        {
            _position += (int)offset;
        }
    }
}
