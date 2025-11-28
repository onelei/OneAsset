using System;
using System.Collections.Generic;

namespace OneAsset.Runtime.Core
{
    public class CapacityListPool<T>
    {
        private readonly Stack<List<T>> _pool = new Stack<List<T>>();
        private readonly int _poolSize;

        public CapacityListPool(int poolSize = 256)
        {
            _poolSize = poolSize;
        }

        public List<T> Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new List<T>();
        }

        public void Release(List<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list), "List cannot be null.");
            }

            list.Clear();

            if (_pool.Count < _poolSize)
            {
                _pool.Push(list);
            }

            //If the pool has reached the maximum capacity, let the list be garbage collected.
        }
    }
}