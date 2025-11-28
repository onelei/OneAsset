using System;
using System.Collections.Generic;

namespace OneAsset.Runtime.Core
{
    /// <summary>
    /// A pool for HashSet instances with capacity limit to reduce GC allocations
    /// </summary>
    public class CapacityHashSetPool<T>
    {
        private readonly Stack<HashSet<T>> _pool = new Stack<HashSet<T>>();
        private readonly int _poolSize;

        public CapacityHashSetPool(int poolSize = 256)
        {
            _poolSize = poolSize;
        }

        /// <summary>
        /// Get a HashSet from the pool, or create a new one if the pool is empty
        /// </summary>
        public HashSet<T> Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : new HashSet<T>();
        }

        /// <summary>
        /// Return a HashSet to the pool for reuse
        /// </summary>
        public void Release(HashSet<T> hashSet)
        {
            if (hashSet == null)
            {
                throw new ArgumentNullException(nameof(hashSet), "HashSet cannot be null.");
            }

            hashSet.Clear();

            if (_pool.Count < _poolSize)
            {
                _pool.Push(hashSet);
            }

            // If the pool has reached the maximum capacity, let the hashSet be garbage collected.
        }
    }

    /// <summary>
    /// Static HashSet pool for convenient access
    /// </summary>
    public static class HashSetPool<T>
    {
        // Default pool size of 16 is sufficient for most single-threaded scenarios with caching
        private static readonly CapacityHashSetPool<T> Pool = new CapacityHashSetPool<T>(256);

        public static HashSet<T> Get()
        {
            return Pool.Get();
        }

        public static void Release(HashSet<T> hashSet) => Pool.Release(hashSet);
    }
}

