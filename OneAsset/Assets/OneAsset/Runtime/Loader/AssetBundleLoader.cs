using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime.Loader
{
    /// <summary>
    /// Bundle load event data
    /// </summary>
    public class BundleLoadEventArgs
    {
        public string BundleName { get; set; }
        public string PackageName { get; set; }
        public string AssetAddress { get; set; }
        public string AssetPath { get; set; }
        public List<string> Dependencies { get; set; }
        public bool IsAsync { get; set; }
        public long BundleSize { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

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

        // Events for monitoring
        public static event Action<BundleLoadEventArgs> OnBundleLoadStart;
        public static event Action<BundleLoadEventArgs> OnBundleLoadSuccess;
        public static event Action<BundleLoadEventArgs> OnBundleLoadFailed;
        public static event Action<string> OnBundleUnload;


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
        /// 加载资源（泛型接口）
        /// </summary>
        /// <param name="address">资源地址</param>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfoByAddress(address);
            if (bundleInfo == null)
            {
                OneAssetLogger.LogError($"Asset not found: {address}");
                return default(T);
            }

            // 1. 加载依赖
            LoadDependencies(bundleInfo);

            // 2. 加载目标包
            var bundleData = LoadBundleInternal(bundleInfo, address);

            // 3. 从包中加载资源
            if (bundleData != null && bundleData.Bundle != null)
            {
                var assetPath = bundleInfo.GetAssetPath(address);
                return bundleData.Bundle.LoadAsset<T>(assetPath);
            }

            return null;
        }

        /// <summary>
        /// 异步加载资源
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

            // 1. 加载依赖
            await LoadDependenciesAsync(bundleInfo);

            // 2. 加载目标包
            var bundleData = await LoadBundleInternalAsync(bundleInfo, address);

            // 3. 从包中加载资源
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
        /// 卸载资源包
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
        /// 加载单个AB包（包含引用计数逻辑）
        /// </summary>
        private BundleData LoadBundleInternal(BundleInfo bundleInfo, string address = "")
        {
            var bundleName = bundleInfo.name;
            // 检查是否已加载
            if (_loadedBundles.TryGetValue(bundleName, out BundleData data))
            {
                data.RefCount++; // 引用计数 +1
                return data;
            }

            // 触发加载开始事件
            var eventArgs = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = _packageName,
                AssetAddress = address,
                AssetPath = bundleInfo.GetAssetPath(address) ?? "",
                Dependencies = bundleInfo.depends ?? new List<string>(),
                IsAsync = false,
                StartTime = DateTime.Now
            };

            OnBundleLoadStart?.Invoke(eventArgs);

            // 真实加载
            var bundlePath = GetAssetBundlePath(bundleName);
            var bundle = _encryptRule.Decrypt(bundlePath, bundleInfo.crc);

            eventArgs.EndTime = DateTime.Now;

            if (bundle == null)
            {
                eventArgs.ErrorMessage = $"Failed to load bundle from path: {bundlePath}";
                OnBundleLoadFailed?.Invoke(eventArgs);
                OneAssetLogger.LogError($"Failed to load bundle: {bundleName}");
                return null;
            }

            BundleData newData = new BundleData(bundleName, bundle);
            newData.RefCount++; // 初始引用为1
            _loadedBundles.Add(bundleName, newData);

            // 记录文件大小
            try
            {
                var fileInfo = new System.IO.FileInfo(bundlePath);
                eventArgs.BundleSize = fileInfo.Length;
            }
            catch
            {
                // Ignore file info errors
            }

            OnBundleLoadSuccess?.Invoke(eventArgs);

            return newData;
        }

        /// <summary>
        /// 异步加载单个AB包（包含引用计数逻辑）
        /// </summary>
        private async UniTask<BundleData> LoadBundleInternalAsync(BundleInfo bundleInfo, string address = null,
            CancellationToken cancellationToken = default)
        {
            var bundleName = bundleInfo.name;
            // 检查是否已加载
            if (_loadedBundles.TryGetValue(bundleName, out BundleData data))
            {
                data.RefCount++; // 引用计数 +1
                return data;
            }

            // 触发加载开始事件
            var eventArgs = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = _packageName,
                AssetAddress = address,
                AssetPath = bundleInfo.GetAssetPath(address) ?? "",
                Dependencies = bundleInfo.depends ?? new List<string>(),
                IsAsync = true,
                StartTime = DateTime.Now
            };

            OnBundleLoadStart?.Invoke(eventArgs);

            // 真实加载
            var bundlePath = GetAssetBundlePath(bundleName);
            var request = _encryptRule.DecryptAsync(bundlePath, bundleInfo.crc);
            await request.ToUniTask(cancellationToken: cancellationToken);
            var bundle = request.assetBundle;
            eventArgs.EndTime = DateTime.Now;

            if (bundle == null)
            {
                eventArgs.ErrorMessage = $"AssetBundle.LoadFromFileAsync returned null, path: {bundlePath}";
                OnBundleLoadFailed?.Invoke(eventArgs);
                OneAssetLogger.LogError($"Failed to load bundle: {bundleName}");
                return null;
            }

            BundleData newData = new BundleData(bundleName, bundle);
            newData.RefCount++; // 初始引用为1
            _loadedBundles.Add(bundleName, newData);

            // 记录文件大小
            try
            {
                var fileInfo = new System.IO.FileInfo(bundlePath);
                eventArgs.BundleSize = fileInfo.Length;
            }
            catch
            {
                // Ignore file info errors
            }

            OnBundleLoadSuccess?.Invoke(eventArgs);

            return newData;
        }

        /// <summary>
        /// 递归加载依赖
        /// </summary>
        private void LoadDependencies(BundleInfo bundleInfo)
        {
            foreach (var dep in bundleInfo.depends)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(dep);
                LoadBundleInternal(dependBundleInfo); // 依赖包的引用计数也会增加
            }
        }

        /// <summary>
        /// 异步递归加载依赖
        /// </summary>
        private async UniTask LoadDependenciesAsync(BundleInfo bundleInfo,
            CancellationToken cancellationToken = default)
        {
            foreach (var bundleName in bundleInfo.depends)
            {
                var dependBundleInfo = GetBundleInfoByBundleName(bundleName);
                await LoadBundleInternalAsync(dependBundleInfo, string.Empty, cancellationToken); // 依赖包的引用计数也会增加
            }
        }

        /// <summary>
        /// 卸载逻辑（包含引用计数逻辑）
        /// </summary>
        private void UnloadBundleInternal(string bundleName, bool unloadAllLoadedObjects = true)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out BundleData bundleData))
                return;

            bundleData.RefCount--; // 引用计数 -1

            // 如果引用归零，执行真卸载
            if (bundleData.RefCount <= 0)
            {
                // 1. 先处理依赖项的卸载（递归减少依赖包的计数）
                var bundleInfo = GetBundleInfoByBundleName(bundleName);
                foreach (var dep in bundleInfo.depends)
                {
                    UnloadBundleInternal(dep, unloadAllLoadedObjects);
                }

                // 2. 卸载自身
                bundleData.Unload(unloadAllLoadedObjects); // true表示卸载从该包加载的所有Object，慎用，根据需求可能填false
                _loadedBundles.Remove(bundleName);

                OneAssetLogger.Log($"Bundle Unloaded: {bundleName}");
                OnBundleUnload?.Invoke(bundleName);
            }
        }
    }
}