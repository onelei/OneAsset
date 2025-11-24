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
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private readonly Dictionary<string, IEncryptRule> _encryptRules = new Dictionary<string, IEncryptRule>();

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

        private IEncryptRule GetEncryptRule(string packageName)
        {
            IEncryptRule encryptRule = null;
            if (_encryptRules.TryGetValue(packageName, out encryptRule)) return encryptRule;
            if (VirtualManifest.Default.TryGetEncryptRule(packageName, out var ruleKey))
            {
                var ruleType = Type.GetType(ruleKey);
                if (ruleType != null)
                {
                    encryptRule = (IEncryptRule) Activator.CreateInstance(ruleType);
                }
            }
            else
            {
                encryptRule = new EncryptDisable();
            }

            _encryptRules[packageName] = encryptRule;

            return encryptRule;
        }

        public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfoByAddress(assetName);
            if (bundleInfo == null)
                return default(T);
            var bundleName = bundleInfo.name;
            var packageName = bundleInfo.PackageName;
            LoadAssetBundle(packageName, bundleName, assetName, bundleInfo, false);
            foreach (var depend in bundleInfo.depends)
            {
                LoadAssetBundle(packageName, depend);
            }

            var assetPath = bundleInfo.GetAssetPath(assetName);
            return LoadAsset<T>(packageName, bundleName, assetPath);
        }

        public async UniTaskVoid LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfoByAddress(assetName);
            var bundleName = bundleInfo.name;
            var packageName = bundleInfo.PackageName;
            await LoadAssetBundleAsync(packageName, bundleName, assetName, bundleInfo, true);
            foreach (var depend in bundleInfo.depends)
            {
                await LoadAssetBundleAsync(packageName, depend);
            }

            var assetPath = bundleInfo.GetAssetPath(assetName);
            LoadAssetAsync(packageName, bundleName, assetPath, onComplete).Forget();
        }

        public void UnloadAsset(string assetName, bool unloadAllLoadedObjects = true)
        {
            var bundleInfo = GetBundleInfoByAddress(assetName);
            var bundleName = bundleInfo.name;
            UnloadAssetBundle(bundleName);
        }

        private void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = true)
        {
            if (!_loadedAssetBundles.ContainsKey(bundleName)) return;
            var bundle = _loadedAssetBundles[bundleName];
            bundle.Unload(unloadAllLoadedObjects);
            _loadedAssetBundles.Remove(bundleName);

            OnBundleUnload?.Invoke(bundleName);
        }

        // LoadAssetBundle with event
        private AssetBundle LoadAssetBundle(string packageName, string bundleName, string assetAddress = "",
            BundleInfo bundleInfo = null, bool isAsync = false)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            if (bundleInfo == null)
            {
                bundleInfo = GetBundleInfoByBundleName(bundleName);
            }

            if (bundleInfo == null)
            {
                throw new Exception($"Bundle {bundleName} not found");
            }

            var eventArgs = new BundleLoadEventArgs
            {
                BundleName = bundleName,
                PackageName = packageName,
                AssetAddress = assetAddress,
                AssetPath = bundleInfo.GetAssetPath(assetAddress) ?? "",
                Dependencies = bundleInfo.depends ?? new List<string>(),
                IsAsync = isAsync,
                StartTime = DateTime.Now
            };

            OnBundleLoadStart?.Invoke(eventArgs);

            var bundlePath = GetAssetBundlePath(packageName, bundleName);
            var encryptRule = GetEncryptRule(packageName);
            if (encryptRule == null)
            {
                throw new Exception($"No encrypt rule found, packageName = {packageName}");
            }

            var bundle = encryptRule.Decrypt(bundlePath, bundleInfo.crc);

            eventArgs.EndTime = DateTime.Now;

            if (bundle != null)
            {
                _loadedAssetBundles.Add(bundleName, bundle);

                try
                {
                    var fileInfo = new System.IO.FileInfo(bundlePath);
                    eventArgs.BundleSize = fileInfo.Length;
                }
                catch
                {
                }

                OnBundleLoadSuccess?.Invoke(eventArgs);
            }
            else
            {
                eventArgs.ErrorMessage = $"Failed to load from path: {bundlePath}";
                OnBundleLoadFailed?.Invoke(eventArgs);
            }

            return bundle;
        }

        // LoadAssetBundleAsync with event
        private async UniTask<AssetBundle> LoadAssetBundleAsync(string packageName, string bundleName,
            string assetAddress = "", BundleInfo bundleInfo = null, bool invokeEvent = false,
            CancellationToken cancellationToken = default)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            if (bundleInfo == null)
            {
                bundleInfo = GetBundleInfoByBundleName(bundleName);
            }

            if (bundleInfo == null)
            {
                throw new Exception($"Bundle {bundleName} not found");
            }

            BundleLoadEventArgs eventArgs = null;
            if (invokeEvent)
            {
                eventArgs = new BundleLoadEventArgs
                {
                    BundleName = bundleName,
                    PackageName = packageName,
                    AssetAddress = assetAddress,
                    AssetPath = bundleInfo.GetAssetPath(assetAddress) ?? "",
                    Dependencies = bundleInfo.depends ?? new List<string>(),
                    IsAsync = true,
                    StartTime = DateTime.Now
                };

                OnBundleLoadStart?.Invoke(eventArgs);
            }

            var bundlePath = GetAssetBundlePath(packageName, bundleName);
            var encryptRule = GetEncryptRule(packageName);
            if (encryptRule == null)
            {
                throw new Exception($"No encrypt rule found, packageName = {packageName}");
            }

            AssetBundle bundle = null;

            // 根据加密规则类型使用不同的加载方式
            var request = encryptRule.DecryptAsync(bundlePath, bundleInfo.crc);
            await request.ToUniTask(cancellationToken: cancellationToken);
            bundle = request.assetBundle;

            if (invokeEvent)
                eventArgs.EndTime = DateTime.Now;

            if (bundle != null)
            {
                _loadedAssetBundles.Add(bundleName, bundle);

                if (invokeEvent)
                {
                    try
                    {
                        var fileInfo = new System.IO.FileInfo(bundlePath);
                        eventArgs.BundleSize = fileInfo.Length;
                    }
                    catch
                    {
                    }

                    OnBundleLoadSuccess?.Invoke(eventArgs);
                }
            }
            else
            {
                OneAssetLogger.LogError($"Failed to load AssetBundle: {bundleName}");

                if (invokeEvent)
                {
                    eventArgs.ErrorMessage = $"AssetBundle.LoadFromFileAsync returned null, path: {bundlePath}";
                    OnBundleLoadFailed?.Invoke(eventArgs);
                }
            }

            return bundle;
        }

        // LoadAsset
        private T LoadAsset<T>(string packageName, string bundleName, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = LoadAssetBundle(packageName, bundleName);
            if (bundle != null)
            {
                return bundle.LoadAsset<T>(assetName);
            }

            return null;
        }

        // LoadAssetAsync
        private async UniTaskVoid LoadAssetAsync<T>(string packageName, string bundleName, string assetName,
            Action<T> onComplete)
            where T : UnityEngine.Object
        {
            var bundle = await LoadAssetBundleAsync(packageName, bundleName);
            if (bundle != null)
            {
                var asset = await bundle.LoadAssetAsync<T>(assetName);
                onComplete?.Invoke(asset as T);
            }
            else
            {
                onComplete?.Invoke(null);
            }
        }

        private string GetAssetBundlePath(string packageName, string bundleName)
        {
#if UNITY_EDITOR
            return $"{OneAssetSetting.GetAssetBundlesRootPath()}/{packageName}/{bundleName}";
#else
            return $"{Application.streamingAssetsPath}/{OneAssetSetting.GetPlatformFolderForAssetBundles()}/{packageName}/{bundleName}";
#endif
        }
    }
}