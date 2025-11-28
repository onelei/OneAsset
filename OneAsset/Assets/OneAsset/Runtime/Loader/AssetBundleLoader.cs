using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Core;
using OneAsset.Runtime.Manifest;
using OneAsset.Runtime.Monitor;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime.Loader
{
    public class AssetBundleLoader : ILoader
    {
        private readonly Dictionary<string, BundleData> _loadedBundles = new Dictionary<string, BundleData>();
        private readonly HashSet<string> _loadingBundles = new HashSet<string>(); // Bundles currently being loaded, used to prevent concurrent duplicate loading
        private readonly string _packageName;
        private readonly IEncryptRule _encryptRule;

        /// <summary>
        /// Auto release check interval (seconds)
        /// </summary>
        public float AutoReleaseCheckInterval = 30f;

        /// <summary>
        /// Unused bundle expiration time (seconds). Bundles exceeding this time with no asset references will be automatically released
        /// </summary>
        public float UnusedBundleExpireTime = 60f;

        public AssetBundleLoader(string packageName, IEncryptRule encryptRule)
        {
            _packageName = packageName;
            _encryptRule = encryptRule;
        }

        private BundleInfo GetBundleInfoByAddress(string address)
        {
            return VirtualManifest.Default.TryGetBundleInfoByAddress(address, out var bundleInfo) ? bundleInfo : null;
        }

        private BundleInfo GetBundleInfoByBundleName(string bundleName)
        {
            return VirtualManifest.Default.TryGetBundleInfoByBundleName(bundleName, out var bundleInfo)
                ? bundleInfo
                : null;
        }

        private string GetAssetBundlePath(string bundleName)
        {
#if UNITY_EDITOR
            return $"{OneAssetSetting.GetAssetBundlesRootPath()}/{_packageName}/{bundleName}";
#else
            return $"{Application.streamingAssetsPath}/{OneAssetSetting.GetPlatformFolderForAssetBundles()}/{_packageName}/{bundleName}";
#endif
        }


        /// <summary>
        /// Load asset (generic interface)
        /// </summary>
        /// <param name="address">Asset address</param>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfoByAddress(address);
            if (bundleInfo == null)
            {
                OneAssetLogger.LogError($"Asset not found: {address}");
                return default(T);
            }

            // 1. Load dependencies
            LoadDependencies(bundleInfo);

            // 2. Load target bundle
            var bundleData = LoadBundleInternal(bundleInfo, address);

            // 3. Load asset from bundle
            if (bundleData != null && bundleData.Bundle != null)
            {
                var assetPath = bundleInfo.GetAssetPath(address);
                var asset = bundleData.Bundle.LoadAsset<T>(assetPath);

                // Track loaded asset for later determining whether the bundle can be released
                bundleData.TrackAsset(asset);
                bundleData.Touch();

                return asset;
            }

            return null;
        }

        /// <summary>
        /// Load asset asynchronously
        /// </summary>
        public async UniTaskVoid LoadAssetAsync<T>(string address, Action<T> onComplete) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfoByAddress(address);
            if (bundleInfo == null)
            {
                OneAssetLogger.LogError($"Asset not found: {address}");
                onComplete?.Invoke(null);
                return;
            }

            // 1. Load dependencies
            await LoadDependenciesAsync(bundleInfo);

            // 2. Load target bundle
            var bundleData = await LoadBundleInternalAsync(bundleInfo, address);

            // 3. Load asset from bundle
            if (bundleData != null && bundleData.Bundle != null)
            {
                var assetPath = bundleInfo.GetAssetPath(address);
                var asset = await bundleData.Bundle.LoadAssetAsync<T>(assetPath);

                // Track loaded asset for later determining whether the bundle can be released
                bundleData.TrackAsset(asset);
                bundleData.Touch();

                onComplete?.Invoke(asset as T);
            }
            else
            {
                onComplete?.Invoke(null);
            }
        }

        /// <summary>
        /// Unload asset bundle
        /// </summary>
        public void UnloadAsset(string address, bool unloadAllLoadedObjects = true)
        {
            var bundleInfo = GetBundleInfoByAddress(address);
            if (bundleInfo == null)
            {
                OneAssetLogger.LogWarning($"Asset not found for unload: {address}");
                return;
            }

            var bundleName = bundleInfo.name;
            UnloadBundleInternal(bundleName, unloadAllLoadedObjects);
        }

        /// <summary>
        /// Load a single AssetBundle (with reference counting logic)
        /// </summary>
        private BundleData LoadBundleInternal(BundleInfo bundleInfo, string address = "")
        {
            var bundleName = bundleInfo.name;
            // Check if already loaded
            if (_loadedBundles.TryGetValue(bundleName, out BundleData data))
            {
                data.RefCount++; // Increment reference count
                return data;
            }

            // Record load start
            var assetPath = bundleInfo.GetAssetPath(address) ?? "";
            var dependencies = bundleInfo.depends ?? new List<string>();
            var startTime = DateTime.Now;

            AssetBundleMonitor.RecordLoadStart(bundleName, _packageName, address, assetPath, dependencies, false);

            // Actual loading
            var bundlePath = GetAssetBundlePath(bundleName);
            var bundle = _encryptRule.Decrypt(bundlePath, bundleInfo.crc);

            if (bundle == null)
            {
                var errorMessage = $"Failed to load bundle from path: {bundlePath}";
                AssetBundleMonitor.RecordLoadFailed(bundleName, _packageName, address, assetPath, dependencies, false,
                    startTime, errorMessage);
                OneAssetLogger.LogError($"Failed to load bundle: {bundleName}");
                return null;
            }

            BundleData newData = new BundleData(bundleName, bundle);
            newData.RefCount++; // Initial reference count is 1
            _loadedBundles.Add(bundleName, newData);
            AssetBundleMonitor.RecordLoadSuccess(bundleName, _packageName, address, assetPath, dependencies, false,
                startTime, bundlePath);
            return newData;
        }

        /// <summary>
        /// Load a single AssetBundle asynchronously (with reference counting logic and concurrency protection)
        /// </summary>
        private async UniTask<BundleData> LoadBundleInternalAsync(BundleInfo bundleInfo, string address = null,
            CancellationToken cancellationToken = default)
        {
            var bundleName = bundleInfo.name;

            // Check if already loaded
            if (_loadedBundles.TryGetValue(bundleName, out BundleData data))
            {
                data.RefCount++; // Increment reference count
                data.Touch();
                return data;
            }

            // Concurrency protection: if currently loading, wait for completion
            if (_loadingBundles.Contains(bundleName))
            {
                // Wait until loading is complete
                await UniTask.WaitUntil(() => !_loadingBundles.Contains(bundleName),
                    cancellationToken: cancellationToken);

                // Check again after loading is complete
                if (_loadedBundles.TryGetValue(bundleName, out data))
                {
                    data.RefCount++;
                    data.Touch();
                    return data;
                }

                // If loading failed, return null
                return null;
            }

            // Mark as currently loading
            _loadingBundles.Add(bundleName);

            try
            {
                // Record load start
                var assetPath = bundleInfo.GetAssetPath(address) ?? "";
                var dependencies = bundleInfo.depends ?? new List<string>();
                var startTime = DateTime.Now;

                AssetBundleMonitor.RecordLoadStart(bundleName, _packageName, address, assetPath, dependencies, true);

                // Actual loading
                var bundlePath = GetAssetBundlePath(bundleName);
                var request = _encryptRule.DecryptAsync(bundlePath, bundleInfo.crc);
                await request.ToUniTask(cancellationToken: cancellationToken);
                var bundle = request.assetBundle;

                if (bundle == null)
                {
                    var errorMessage = $"AssetBundle.LoadFromFileAsync returned null, path: {bundlePath}";
                    AssetBundleMonitor.RecordLoadFailed(bundleName, _packageName, address, assetPath, dependencies,
                        true,
                        startTime, errorMessage);
                    OneAssetLogger.LogError($"Failed to load bundle: {bundleName}");
                    return null;
                }

                BundleData newData = new BundleData(bundleName, bundle);
                newData.RefCount++; // Initial reference count is 1
                _loadedBundles.Add(bundleName, newData);
                AssetBundleMonitor.RecordLoadSuccess(bundleName, _packageName, address, assetPath, dependencies, true,
                    startTime, bundlePath);
                return newData;
            }
            finally
            {
                // Remove loading flag regardless of success or failure
                _loadingBundles.Remove(bundleName);
            }
        }

        /// <summary>
        /// Load all dependencies (using cached GetAllDependence)
        /// </summary>
        private void LoadDependencies(BundleInfo bundleInfo)
        {
            // Use GetAllDependence to get all dependencies (already ordered correctly for loading, with caching)
            var allDeps = VirtualManifest.Default.GetAllDependence(bundleInfo.name);
            if (allDeps == null || allDeps.Count == 0) return;

            foreach (var dep in allDeps)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(dep);
                if (dependBundleInfo != null)
                {
                    LoadBundleInternal(dependBundleInfo); // Reference count of dependency bundles will also increase
                }
            }
        }

        /// <summary>
        /// Load all dependencies asynchronously (using cached GetAllDependence)
        /// </summary>
        private async UniTask LoadDependenciesAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            // Use GetAllDependence to get all dependencies (already ordered correctly for loading, with caching)
            var allDeps = VirtualManifest.Default.GetAllDependence(bundleInfo.name);
            if (allDeps == null || allDeps.Count == 0) return;

            foreach (var bundleName in allDeps)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(bundleName);
                if (dependBundleInfo != null)
                {
                    await LoadBundleInternalAsync(dependBundleInfo, string.Empty,
                        cancellationToken); // Reference count of dependency bundles will also increase
                }
            }
        }

        /// <summary>
        /// Unload logic (with reference counting logic)
        /// </summary>
        private void UnloadBundleInternal(string bundleName, bool unloadAllLoadedObjects = true)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out BundleData bundleData))
                return;

            bundleData.RefCount--; // Decrement reference count

            // If reference count reaches zero, perform actual unload
            if (bundleData.RefCount <= 0)
            {
                // 1. First handle unloading of dependencies (recursively decrease dependency bundle counts)
                var bundleInfo = GetBundleInfoByBundleName(bundleName);
                if (bundleInfo?.depends != null)
                {
                    foreach (var dep in bundleInfo.depends)
                    {
                        UnloadBundleInternal(dep, unloadAllLoadedObjects);
                    }
                }

                // 2. Unload itself
                bundleData.Unload(
                    unloadAllLoadedObjects); // true means unload all Objects loaded from this bundle, use with caution, may use false depending on requirements
                _loadedBundles.Remove(bundleName);

                OneAssetLogger.Log($"Bundle Unloaded: {bundleName}");
                AssetBundleMonitor.RecordUnload(bundleName);
            }
        }

        #region Extended Features

        /// <summary>
        /// Get the count of loaded bundles
        /// </summary>
        public int GetLoadedBundleCount() => _loadedBundles.Count;

        /// <summary>
        /// Get all loaded bundle names
        /// </summary>
        public IEnumerable<string> GetLoadedBundleNames() => _loadedBundles.Keys;

        /// <summary>
        /// Get the reference count of a bundle
        /// </summary>
        public int GetBundleRefCount(string bundleName)
        {
            return _loadedBundles.TryGetValue(bundleName, out var data) ? data.RefCount : 0;
        }

        /// <summary>
        /// Check if the bundle has alive asset references
        /// </summary>
        public bool HasAliveAssets(string bundleName)
        {
            return _loadedBundles.TryGetValue(bundleName, out var data) && data.HasAliveAssets();
        }

        /// <summary>
        /// Release unused bundles
        /// </summary>
        /// <param name="immediate">
        /// true: Immediately release all bundles with no asset references (suitable for scene switching, low memory warnings)
        /// false: Only release bundles that have exceeded expiration time (suitable for periodic checks)
        /// </param>
        /// <param name="unloadAllLoadedObjects">Whether to also unload all objects loaded from the bundle</param>
        /// <returns>Number of bundles released</returns>
        public void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
            var bundlesToUnload = ListPool<string>.Get();
            var now = DateTime.Now;

            foreach (var kvp in _loadedBundles)
            {
                var bundleData = kvp.Value;

                // Required condition: no alive asset references
                if (bundleData.HasAliveAssets())
                    continue;

                if (immediate)
                {
                    // Immediate mode: release directly
                    bundlesToUnload.Add(kvp.Key);
                }
                else
                {
                    // Delayed mode: check if expiration time is exceeded
                    var idleTime = (now - bundleData.LastAccessTime).TotalSeconds;
                    if (idleTime > UnusedBundleExpireTime)
                    {
                        bundlesToUnload.Add(kvp.Key);
                    }
                }
            }

            foreach (var bundleName in bundlesToUnload)
            {
                ForceUnloadBundle(bundleName, unloadAllLoadedObjects);
            }

            var count = bundlesToUnload.Count;
            if (count > 0)
            {
                OneAssetLogger.Log($"UnloadUnusedBundles: Released {count} bundles (immediate={immediate})");
            }

            ListPool<string>.Release(bundlesToUnload);
        }

        /// <summary>
        /// Force unload specified bundle (ignoring reference count)
        /// </summary>
        public void ForceUnloadBundle(string bundleName, bool unloadAllLoadedObjects = true)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out var bundleData))
                return;

            bundleData.Unload(unloadAllLoadedObjects);
            _loadedBundles.Remove(bundleName);
            OneAssetLogger.Log($"Bundle Force Unloaded: {bundleName}");
            AssetBundleMonitor.RecordUnload(bundleName);
        }

        /// <summary>
        /// Unload all loaded bundles
        /// </summary>
        public void UnloadAll(bool unloadAllLoadedObjects = true)
        {
            var bundleNames = new List<string>(_loadedBundles.Keys);
            foreach (var bundleName in bundleNames)
            {
                ForceUnloadBundle(bundleName, unloadAllLoadedObjects);
            }

            _loadedBundles.Clear();
            _loadingBundles.Clear();
            OneAssetLogger.Log("All bundles unloaded");
        }

        /// <summary>
        /// Get bundle status information (for debugging)
        /// </summary>
        public string GetBundleStatusInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Bundle Status ({_loadedBundles.Count} loaded) ===");
            foreach (var kvp in _loadedBundles)
            {
                var data = kvp.Value;
                var aliveCount = data.GetAliveAssetCount();
                var idleTime = (DateTime.Now - data.LastAccessTime).TotalSeconds;
                sb.AppendLine(
                    $"  {kvp.Key}: RefCount={data.RefCount}, AliveAssets={aliveCount}, IdleTime={idleTime:F1}s");
            }

            return sb.ToString();
        }

        #endregion
    }
}