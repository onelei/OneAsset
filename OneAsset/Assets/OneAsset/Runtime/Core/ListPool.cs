using System.Collections.Generic;

namespace OneAsset.Runtime.Core
{
    public static class ListPool<T>
    {
        private static readonly CapacityListPool<T> Pool = new CapacityListPool<T>(1024);

        public static List<T> Get()
        {
            return Pool.Get();
        }

        public static void Release(List<T> list) => Pool.Release(list);
    }
}