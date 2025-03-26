using System.Collections.Generic;

namespace OneAsset.Runtime.Core
{
    public static class ListPool<T>
    {
        private static readonly CapacityListPool<T> Pool = new CapacityListPool<T>(int.MaxValue);

        public static List<T> Get()
        {
            return Pool.Get();
        }

        public static void Release(List<T> list) => Pool.Release(list);
    }
}