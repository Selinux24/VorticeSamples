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
        private readonly List<int> indices;
        private readonly List<T> array;
        private int nextFreeIndex = -1;
        private int size = 0;

        /// <summary>
        /// Number of elements in the list.
        /// </summary>
        public int Size => size;
        /// <summary>
        /// Maximum number of elements that can be added to the list.
        /// </summary>
        public int Capacity => array.Capacity;
        /// <summary>
        /// Indicates if the list is empty.
        /// </summary>
        public bool Empty => size == 0;
        /// <summary>
        /// Access an element by its index.
        /// </summary>
        /// <param name="id"></param>
        public T this[int id]
        {
            get
            {
                Debug.Assert(id < array.Count && !AlreadyRemoved(id));
                return array[id];
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
        public int Add(T value)
        {
            int id;
            if (nextFreeIndex == -1)
            {
                id = array.Count;
                array.Add(value);
                indices.Add(id);
            }
            else
            {
                id = nextFreeIndex;
                Debug.Assert(id < array.Count && AlreadyRemoved(id));
                array[id] = value;
                nextFreeIndex = indices[id];
                indices[id] = id;
            }
            size++;
            return id;
        }
        /// <summary>
        /// Removes an element from the list.
        /// </summary>
        /// <param name="id">Id</param>
        public void Remove(int id)
        {
            Debug.Assert(id < array.Count && !AlreadyRemoved(id));
            (array[id] as IDisposable)?.Dispose();
            array[id] = default;
            indices[id] = nextFreeIndex;
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
            nextFreeIndex = -1;
            size = 0;
        }

        private bool AlreadyRemoved(int id)
        {
            return indices[id] == -1;
        }
    }
}
