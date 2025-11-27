using UnityEngine;

namespace OneAsset.Runtime.Loader
{
    public class BundleData
    {
        public AssetBundle Bundle;
        public int RefCount;
        public string BundleName;

        public BundleData(string name, AssetBundle ab)
        {
            BundleName = name;
            Bundle = ab;
            RefCount = 0;
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
            if (Bundle == null) return;
            Bundle.Unload(unloadAllLoadedObjects);
            Bundle = null;
        }
    }
}