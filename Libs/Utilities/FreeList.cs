using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UtilitiesTests")]
namespace Utilities
{
    /// <summary>
    /// FreeList is a structure that allows to add and remove elements from a collection, 
    /// quickly, and ensuring that existing elements remain in the same position in which they were added.
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <remarks>
    /// Create a new FreeList with a specified capacity.
    /// </remarks>
    /// <param name="count">Capacity</param>
    public class FreeList<T>(uint count)
    {
        struct Idx(uint index)
        {
            public uint Index = index;
            public bool Removed = false;
        }

        private const uint InitialCapacity = 1024;

        private Idx[] indices = new Idx[count];
        private T[] array = new T[count];
        private uint nextFreeIndex = uint.MaxValue;
        private uint size = 0;

        /// <summary>
        /// Number of elements in the list.
        /// </summary>
        public uint Size => size;
        /// <summary>
        /// Maximum number of elements that can be added to the list.
        /// </summary>
        public uint Capacity => (uint)array.LongLength;
        /// <summary>
        /// Indicates if the list is empty.
        /// </summary>
        public bool Empty => size == 0;
        /// <summary>
        /// Access an element by its index.
        /// </summary>
        /// <param name="id">Index</param>
        public T this[uint id]
        {
            get
            {
                Debug.Assert(id < Capacity && !AlreadyRemoved(id));
                return array[(int)id];
            }
            set
            {
                Debug.Assert(id < Capacity && !AlreadyRemoved(id));
                array[(int)id] = value;
            }
        }

        /// <summary>
        /// Create a new FreeList.
        /// </summary>
        public FreeList() : this(InitialCapacity)
        {

        }

        /// <summary>
        /// Adds a new element to the list.
        /// </summary>
        /// <param name="value">Value</param>
        public uint Add(T value)
        {
            uint id;
            if (nextFreeIndex == uint.MaxValue)
            {
                if (size >= Capacity)
                {
                    //Grow the list
                    int newCapacity = (int)Capacity * 2;
                    Array.Resize(ref array, newCapacity);
                    Array.Resize(ref indices, newCapacity);
                }

                id = size;
                array[(int)id] = value;
                indices[(int)id] = new(id);
            }
            else
            {
                id = nextFreeIndex;
                Debug.Assert(id < Capacity && AlreadyRemoved(id));
                array[(int)id] = value;

                var idx = indices[(int)id];
                nextFreeIndex = idx.Index;
                idx.Removed = false;
                indices[(int)id] = idx;
            }
            size++;
            return id;
        }
        /// <summary>
        /// Removes an element from the list.
        /// </summary>
        /// <param name="id">Id</param>
        public void Remove(uint id)
        {
            Debug.Assert(id < Capacity && !AlreadyRemoved(id));
            (array[id] as IDisposable)?.Dispose();
            array[id] = default;

            var idx = indices[(int)id];
            idx.Index = nextFreeIndex;
            idx.Removed = true;
            indices[id] = idx;

            nextFreeIndex = id;
            size--;
        }
        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            nextFreeIndex = uint.MaxValue;
            size = 0;
        }

        /// <summary>
        /// Gets the first alive element in the list.
        /// </summary>
        public bool First(out T first)
        {
            for (uint i = 0; i < Capacity; i++)
            {
                if (!AlreadyRemoved(i))
                {
                    first = array[indices[i].Index];
                    return true;
                }
            }

            first = default;
            return false;
        }

        /// <summary>
        /// Gets whether the specified index element has already been removed.
        /// </summary>
        /// <param name="id">Id</param>
        private bool AlreadyRemoved(uint id)
        {
            return indices[id].Removed;
        }
    }
}
