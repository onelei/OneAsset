using System;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace OneAsset.Runtime.Loader
{
    public class BundleData
    {
        public AssetBundle Bundle;
        public int RefCount;
        public string BundleName;
        public DateTime LastAccessTime;

        // Used to track whether assets loaded from this bundle are still in use
        private readonly List<WeakReference<UObject>> _loadedAssetRefs =
            new List<WeakReference<UObject>>();

        public BundleData(string name, AssetBundle ab)
        {
            BundleName = name;
            Bundle = ab;
            RefCount = 0;
            LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// Update last access time
        /// </summary>
        public void Touch()
        {
            LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// Track loaded asset
        /// </summary>
        public void TrackAsset(UObject asset)
        {
            if (asset != null)
            {
                _loadedAssetRefs.Add(new WeakReference<UObject>(asset));
            }
        }

        /// <summary>
        /// Check if any assets are still in use
        /// </summary>
        public bool HasAliveAssets()
        {
            // Clean up expired weak references
            _loadedAssetRefs.RemoveAll(weakRef => !weakRef.TryGetTarget(out _));
            return _loadedAssetRefs.Count > 0;
        }

        /// <summary>
        /// Get the count of alive assets
        /// </summary>
        public int GetAliveAssetCount()
        {
            _loadedAssetRefs.RemoveAll(weakRef => !weakRef.TryGetTarget(out _));
            return _loadedAssetRefs.Count;
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
            if (Bundle == null) return;
            Bundle.Unload(unloadAllLoadedObjects);
            Bundle = null;
            _loadedAssetRefs.Clear();
        }
    }
}