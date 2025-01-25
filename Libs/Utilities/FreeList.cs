using System;
using System.Collections.Generic;
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
    public class FreeList<T>
    {
        private readonly List<uint> indices;
        private readonly List<T> array;
        private uint nextFreeIndex = uint.MaxValue;
        private uint size = 0;

        /// <summary>
        /// Number of elements in the list.
        /// </summary>
        public uint Size => size;
        /// <summary>
        /// Maximum number of elements that can be added to the list.
        /// </summary>
        public uint Capacity => (uint)array.Capacity;
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
                Debug.Assert(id < array.Count && !AlreadyRemoved(id));
                return array[(int)id];
            }
        }

        /// <summary>
        /// Create a new FreeList.
        /// </summary>
        public FreeList()
        {
            indices = [];
            array = [];
        }
        /// <summary>
        /// Create a new FreeList with a specified capacity.
        /// </summary>
        /// <param name="count">Capacity</param>
        public FreeList(int count)
        {
            indices = new(count);
            array = new(count);
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
                id = (uint)array.Count;
                array.Add(value);
                indices.Add(id);
            }
            else
            {
                id = nextFreeIndex;
                Debug.Assert(id < array.Count && AlreadyRemoved(id));
                array[(int)id] = value;
                nextFreeIndex = indices[(int)id];
                indices[(int)id] = id;
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
            Debug.Assert(id < array.Count && !AlreadyRemoved(id));
            (array[(int)id] as IDisposable)?.Dispose();
            array[(int)id] = default;
            indices[(int)id] = nextFreeIndex;
            nextFreeIndex = id;
            size--;
        }
        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            indices.Clear();
            array.Clear();
            nextFreeIndex = uint.MaxValue;
            size = 0;
        }


        public bool First(out T first)
        {
            for (uint i = 0; i < indices.Count; i++)
            {
                if (!AlreadyRemoved(i))
                {
                    first = array[(int)indices[(int)i]];
                    return true;
                }
            }

            first = default;
            return false;
        }

        private bool AlreadyRemoved(uint id)
        {
            return indices[(int)id] == uint.MaxValue;
        }
    }
}
