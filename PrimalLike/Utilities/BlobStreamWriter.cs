using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PrimalLike.Utilities
{
    /// <summary>
    /// Blob stream writer.
    /// </summary>
    public class BlobStreamWriter
    {
        private readonly IntPtr _buffer;
        private int _position;
        private readonly int _bufferSize;

        /// <summary>
        /// Buffer start pointer
        /// </summary>
        public IntPtr BufferStart => _buffer;
        /// <summary>
        /// Buffer end pointer
        /// </summary>
        public IntPtr BufferEnd => _buffer;
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
        /// <param name="bufferSize">Buffer size</param>
        public BlobStreamWriter(IntPtr buffer, int bufferSize)
        {
            Debug.Assert(buffer != IntPtr.Zero && bufferSize > 0);
            _buffer = buffer;
            _position = 0;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// This method is intended to write primitive types (e.g. int, float, bool)
        /// </summary>
        /// <typeparam name="T">Primitive types</typeparam>
        /// <param name="value">Value</param>
        public void Write<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            Debug.Assert(_position + size <= _bufferSize);
            Marshal.StructureToPtr(value, _buffer + _position, false);
            _position += size;
        }
        /// <summary>
        /// Writes bytes into 'buffer'.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        public void Write(byte[] buffer)
        {
            int length = buffer.Length;
            Debug.Assert(_position + length <= _bufferSize);
            Marshal.Copy(buffer, 0, _buffer + _position, length);
            _position += length;
        }
        /// <summary>
        /// Writes 'length' bytes into 'buffer'.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Buffer length</param>
        public void Write(IntPtr buffer, int length)
        {
            Debug.Assert(_position + length <= _bufferSize);
            byte[] tempBuffer = new byte[length];
            Marshal.Copy(buffer, tempBuffer, 0, length);
            Marshal.Copy(tempBuffer, 0, _buffer + _position, length);
            _position += length;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(int offset)
        {
            Debug.Assert(_position + offset <= _bufferSize);
            _position += offset;
        }
    }
}
