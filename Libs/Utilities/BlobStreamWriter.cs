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
        /// <summary>
        /// Start buffer pointer
        /// </summary>
        private readonly IntPtr buffer;
        /// <summary>
        /// Buffer size
        /// </summary>
        private readonly int bufferSize;
        /// <summary>
        /// Current buffer pointer
        /// </summary>
        private IntPtr position;

        /// <summary>
        /// Buffer start pointer
        /// </summary>
        public IntPtr BufferStart => buffer;
        /// <summary>
        /// Buffer end pointer
        /// </summary>
        public IntPtr BufferEnd => buffer + bufferSize;
        /// <summary>
        /// Current position
        /// </summary>
        public IntPtr Position { get => position; set => position = value; }
        /// <summary>
        /// Current offset
        /// </summary>
        public int Offset => (int)(position - buffer);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="bufferSize">Buffer size</param>
        public BlobStreamWriter(IntPtr buffer, int bufferSize)
        {
            Debug.Assert(buffer != IntPtr.Zero && bufferSize > 0);
            this.buffer = buffer;
            this.bufferSize = bufferSize;
            position = buffer;
        }

        /// <summary>
        /// Writes 'value' into 'buffer'.
        /// </summary>
        /// <param name="value">String value</param>
        public void Write(string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            int size = buffer.Length;
            Debug.Assert(Offset + size <= bufferSize);
            Write(size);
            Marshal.Copy(buffer, 0, position, size);
            position += size;
        }
        /// <summary>
        /// This method is intended to write primitive types (e.g. int, float, bool)
        /// </summary>
        /// <typeparam name="T">Primitive types</typeparam>
        /// <param name="value">Value</param>
        public void Write<T>(T value) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            Debug.Assert(Offset + size <= bufferSize);
            Marshal.StructureToPtr(value, position, false);
            position += size;
        }
        /// <summary>
        /// Writes 'array' into 'buffer'.
        /// </summary>
        /// <typeparam name="T">Array type</typeparam>
        /// <param name="array">Array</param>
        public void Write<T>(T[] array) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>() * array.Length;
            Debug.Assert(Offset + size <= bufferSize);
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
            Debug.Assert(Offset + size <= bufferSize);
            Marshal.Copy(buffer, 0, position, size);
            position += size;
        }
        /// <summary>
        /// Writes 'length' bytes into 'buffer'.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Buffer length</param>
        public void Write(IntPtr buffer, int length)
        {
            Debug.Assert(Offset + length <= bufferSize);
            byte[] tempBuffer = new byte[length];
            Marshal.Copy(buffer, tempBuffer, 0, length);
            Marshal.Copy(tempBuffer, 0, position, length);
            position += length;
        }
        /// <summary>
        /// Skips 'offset' bytes.
        /// </summary>
        /// <param name="offset">Offset</param>
        public void Skip(int offset)
        {
            Debug.Assert(Offset + offset <= bufferSize);
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

        /// <summary>
        /// Saves the buffer to a file.
        /// </summary>
        /// <param name="fileName">File name</param>
        public void SaveToFile(string fileName)
        {
            byte[] buffer = new byte[bufferSize];
            Marshal.Copy(this.buffer, buffer, 0, bufferSize);
            System.IO.File.WriteAllBytes(fileName, buffer);
        }
    }
}
