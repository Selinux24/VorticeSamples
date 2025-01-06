using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Utilities
{
    /// <summary>
    /// Blob stream writer.
    /// </summary>
    public class BlobStreamWriter
    {
        private readonly IntPtr _buffer;
        private IntPtr _position;
        private readonly int _bufferSize;

        /// <summary>
        /// Buffer start pointer
        /// </summary>
        public IntPtr BufferStart => _buffer;
        /// <summary>
        /// Buffer end pointer
        /// </summary>
        public IntPtr BufferEnd => _buffer + _bufferSize;
        /// <summary>
        /// Current position
        /// </summary>
        public IntPtr Position => _position;
        /// <summary>
        /// Current offset
        /// </summary>
        public int Offset => (int)(_position - _buffer);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="bufferSize">Buffer size</param>
        public BlobStreamWriter(IntPtr buffer, int bufferSize)
        {
            Debug.Assert(buffer != IntPtr.Zero && bufferSize > 0);
            _buffer = buffer;
            _position = buffer;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Writes 'value' into 'buffer'.
        /// </summary>
        /// <param name="value">String value</param>
        public void Write(string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            int size = buffer.Length;
            Debug.Assert(Offset + size <= _bufferSize);
            Write(size);
            Marshal.Copy(buffer, 0, _position, size);
            _position += size;
        }
        /// <summary>
        /// This method is intended to write primitive types (e.g. int, float, bool)
        /// </summary>
        /// <typeparam name="T">Primitive types</typeparam>
        /// <param name="value">Value</param>
        public void Write<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            Debug.Assert(Offset + size <= _bufferSize);
            Marshal.StructureToPtr(value, _position, false);
            _position += size;
        }
        /// <summary>
        /// Writes 'array' into 'buffer'.
        /// </summary>
        /// <typeparam name="T">Array type</typeparam>
        /// <param name="array">Array</param>
        public void Write<T>(T[] array) where T : struct
        {
            int size = Marshal.SizeOf<T>() * array.Length;
            Debug.Assert(Offset + size <= _bufferSize);
            byte[] buffer = new byte[size];
            Buffer.BlockCopy(array, 0, buffer, 0, size);
            Write(buffer);
        }
        /// <summary>
        /// Writes bytes into 'buffer'.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        public void Write(byte[] buffer)
        {
            int size = buffer.Length;
            Debug.Assert(Offset + size <= _bufferSize);
            Marshal.Copy(buffer, 0, _position, size);
            _position += size;
        }
        /// <summary>
        /// Writes 'length' bytes into 'buffer'.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Buffer length</param>
        public void Write(IntPtr buffer, int length)
        {
            Debug.Assert(Offset + length <= _bufferSize);
            byte[] tempBuffer = new byte[length];
            Marshal.Copy(buffer, tempBuffer, 0, length);
            Marshal.Copy(tempBuffer, 0, _position, length);
            _position += length;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(int offset)
        {
            Debug.Assert(Offset + offset <= _bufferSize);
            _position += offset;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(uint offset)
        {
            Debug.Assert(Offset + offset <= _bufferSize);
            _position += (int)offset;
        }
    }
}
