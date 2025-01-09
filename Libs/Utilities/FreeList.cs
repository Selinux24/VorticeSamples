using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Utilities
{
    public class FreeList<T>
    {
        private readonly List<int> indices;
        private readonly List<T> array;
        private int nextFreeIndex = -1;
        private int size = 0;

        public int Size => size;
        public int Capacity => array.Capacity;
        public bool Empty => size == 0;
        public T this[int id]
        {
            get
            {
                Debug.Assert(id < array.Count && !AlreadyRemoved(id));
                return array[id];
            }
        }

        public FreeList()
        {
            indices = [];
            array = [];
        }
        public FreeList(int count)
        {
            indices = new(count);
            array = new(count);
        }

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
        public void Remove(int id)
        {
            Debug.Assert(id < array.Count && !AlreadyRemoved(id));
            (array[id] as IDisposable)?.Dispose();
            array[id] = default;
            indices[id] = nextFreeIndex;
            nextFreeIndex = id;
            size--;
        }
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
