using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using OneAsset.Runtime.Manifest;

namespace OneAsset.Runtime.Loader
{
    public class AssetBundleLoader : ILoader
    {
        private readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();

        private BundleInfo GetBundleInfo(string address)
        {
            return VirtualManifest.Default.TryGetBundleInfo(address, out var bundleInfo) ? bundleInfo : null;
        }

        public string GetAssetBundleName(string assetName)
        {
            return VirtualManifest.Default.GetBundleNameByAddress(assetName);
        }

        public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfo(assetName);
            if (bundleInfo == null)
                return default(T);
            var bundleName = bundleInfo.name;
            var packageName = bundleInfo.PackageName;
            LoadAssetBundle(packageName, bundleName);
            foreach (var depend in bundleInfo.depends)
            {
                LoadAssetBundle(packageName, depend);
            }

            var assetPath = bundleInfo.GetAssetPath(assetName);
            return LoadAsset<T>(packageName, bundleName, assetPath);
        }

        public async UniTaskVoid LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            var bundleInfo = GetBundleInfo(assetName);
            var bundleName = bundleInfo.name;
            var packageName = bundleInfo.PackageName;
            await LoadAssetBundleAsync(packageName, bundleName);
            foreach (var depend in bundleInfo.depends)
            {
                await LoadAssetBundleAsync(packageName, depend);
            }

            var assetPath = bundleInfo.GetAssetPath(assetName);
            LoadAssetAsync(packageName, bundleName, assetPath, onComplete).Forget();
        }

        public void UnloadAsset(string assetName, bool unloadAllLoadedObjects = false)
        {
            var bundleInfo = GetBundleInfo(assetName);
            var bundleName = bundleInfo.name;
            UnloadAssetBundle(bundleName);
        }

        private void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (!_loadedAssetBundles.ContainsKey(bundleName)) return;
            var bundle = _loadedAssetBundles[bundleName];
            bundle.Unload(unloadAllLoadedObjects);
            _loadedAssetBundles.Remove(bundleName);
        }

        // LoadAssetBundle
        private AssetBundle LoadAssetBundle(string packageName, string bundleName)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            var bundlePath = GetAssetBundlePath(packageName, bundleName);
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle != null)
            {
                _loadedAssetBundles.Add(bundleName, bundle);
            }

            return bundle;
        }

        // LoadAssetBundleAsync
        private async UniTask<AssetBundle> LoadAssetBundleAsync(string packageName, string bundleName,
            CancellationToken cancellationToken = default)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            var bundlePath = GetAssetBundlePath(packageName, bundleName);
            using (var request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath))
            {
                var operation = request.SendWebRequest();
                await operation.ToUniTask(cancellationToken: cancellationToken);

                if (request.isHttpError || request.isNetworkError)
                {
                    OneAssetLogger.LogError($"Failed to load AssetBundle: {bundleName}, Error: {request.error}");
                }
                else
                {
                    var bundle = DownloadHandlerAssetBundle.GetContent(request);
                    if (bundle != null)
                    {
                        _loadedAssetBundles.Add(bundleName, bundle);
                        return bundle;
                    }
                }
            }

            return null;
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