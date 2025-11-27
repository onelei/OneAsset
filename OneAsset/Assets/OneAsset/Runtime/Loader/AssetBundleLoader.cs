using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;
using OneAsset.Runtime.Monitor;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime.Loader
{
    public class AssetBundleLoader : ILoader
    {
        private readonly Dictionary<string, BundleData> _loadedBundles = new Dictionary<string, BundleData>();
        private readonly string _packageName;
        private readonly IEncryptRule _encryptRule;

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
                return bundleData.Bundle.LoadAsset<T>(assetPath);
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
        /// Load a single AssetBundle asynchronously (with reference counting logic)
        /// </summary>
        private async UniTask<BundleData> LoadBundleInternalAsync(BundleInfo bundleInfo, string address = null,
            CancellationToken cancellationToken = default)
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

            AssetBundleMonitor.RecordLoadStart(bundleName, _packageName, address, assetPath, dependencies, true);

            // Actual loading
            var bundlePath = GetAssetBundlePath(bundleName);
            var request = _encryptRule.DecryptAsync(bundlePath, bundleInfo.crc);
            await request.ToUniTask(cancellationToken: cancellationToken);
            var bundle = request.assetBundle;

            if (bundle == null)
            {
                var errorMessage = $"AssetBundle.LoadFromFileAsync returned null, path: {bundlePath}";
                AssetBundleMonitor.RecordLoadFailed(bundleName, _packageName, address, assetPath, dependencies, true,
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

        /// <summary>
        /// Recursively load dependencies
        /// </summary>
        private void LoadDependencies(BundleInfo bundleInfo)
        {
            foreach (var dep in bundleInfo.depends)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(dep);
                LoadBundleInternal(dependBundleInfo); // Reference count of dependency bundles will also increase
            }
        }

        /// <summary>
        /// Recursively load dependencies asynchronously
        /// </summary>
        private async UniTask LoadDependenciesAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            foreach (var bundleName in bundleInfo.depends)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(bundleName);
                await LoadBundleInternalAsync(dependBundleInfo, string.Empty,
                    cancellationToken); // Reference count of dependency bundles will also increase
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
                foreach (var dep in bundleInfo.depends)
                {
                    UnloadBundleInternal(dep, unloadAllLoadedObjects);
                }

                // 2. Unload itself
                bundleData.Unload(
                    unloadAllLoadedObjects); // true means unload all Objects loaded from this bundle, use with caution, may use false depending on requirements
                _loadedBundles.Remove(bundleName);

                OneAssetLogger.Log($"Bundle Unloaded: {bundleName}");
                AssetBundleMonitor.RecordUnload(bundleName);
            }
        }
    }
}