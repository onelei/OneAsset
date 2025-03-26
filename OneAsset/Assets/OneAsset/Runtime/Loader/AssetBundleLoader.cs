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
        
        public string GetAssetBundleName(string assetName)
        {
            return VirtualManifest.Default.GetBundleNameByAddress(assetName);
        }

        private List<string> GetAllDependsBundleByAssetName(string assetName)
        {
            return VirtualManifest.Default.GetAllDependsBundleByAddress(assetName);
        }

        public T LoadAsset<T>(string assetName) where T : UnityEngine.Object
        {
            var bundleName = GetAssetBundleName(assetName);
            LoadAssetBundle(bundleName);
            var depends = GetAllDependsBundleByAssetName(assetName);
            foreach (var depend in depends)
            {
                LoadAssetBundle(depend);
            }
            return LoadAssetSync<T>(bundleName, assetName);
        }

        public async UniTaskVoid LoadAssetAsync<T>(string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            var bundleName = GetAssetBundleName(assetName);
            await LoadAssetBundleAsync(bundleName);
            var depends = GetAllDependsBundleByAssetName(assetName);
            foreach (var depend in depends)
            {
                await LoadAssetBundleAsync(depend);
            }
            LoadAssetAsync(bundleName, assetName, onComplete).Forget();
        }
        
        public void UnloadAsset(string assetName, bool unloadAllLoadedObjects = false)
        {
            var bundleName = GetAssetBundleName(assetName);
            UnloadAssetBundle(bundleName);
        }
 
        private void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (!_loadedAssetBundles.ContainsKey(bundleName)) return;
            var bundle = _loadedAssetBundles[bundleName];
            bundle.Unload(unloadAllLoadedObjects);
            _loadedAssetBundles.Remove(bundleName);
        }

        // 同步加载 AssetBundle
        private AssetBundle LoadAssetBundle(string bundleName)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            var bundlePath = GetAssetBundlePath(bundleName);
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle != null)
            {
                _loadedAssetBundles.Add(bundleName, bundle);
            }

            return bundle;
        }

        // 异步加载 AssetBundle
        private async UniTask<AssetBundle> LoadAssetBundleAsync(string bundleName, CancellationToken cancellationToken = default)
        {
            if (_loadedAssetBundles.ContainsKey(bundleName))
            {
                return _loadedAssetBundles[bundleName];
            }

            var bundlePath = GetAssetBundlePath(bundleName);
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
        
        // 同步加载 Asset
        private T LoadAssetSync<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = LoadAssetBundle(bundleName);
            if (bundle != null)
            {
                return bundle.LoadAsset<T>(assetName);
            }

            return null;
        }
        
        // 异步加载 Asset
        private async UniTaskVoid LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            var bundle = await LoadAssetBundleAsync(bundleName);
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
         
        private string GetAssetBundlePath(string bundleName)
        {
            // 根据不同平台获取 AssetBundle 路径
            string platformFolder = GetPlatformFolderForAssetBundles();
            return $"{Application.streamingAssetsPath}/{platformFolder}/{bundleName}";
        }

        private string GetPlatformFolderForAssetBundles()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "OSX";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return string.Empty;
            }
        }
    }
}