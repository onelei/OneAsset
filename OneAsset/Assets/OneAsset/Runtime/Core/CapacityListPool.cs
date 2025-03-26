using System;
using System.Collections.Generic;

namespace OneAsset.Runtime.Core
{
    public class CapacityListPool<T>
    {
        private readonly Stack<List<T>> _pool = new Stack<List<T>>();
        protected int PoolSize;

        public CapacityListPool(int poolSize = 256)
        {
            PoolSize = poolSize;
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

            if (_pool.Count < PoolSize)
            {
                _pool.Push(list);
            }

            //If the pool has reached the maximum capacity, let the list be garbage collected.
        }
    }
}