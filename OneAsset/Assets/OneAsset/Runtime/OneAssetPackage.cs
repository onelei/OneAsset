using System;
using OneAsset.Runtime.Loader;
using OneAsset.Runtime.Rule;

namespace OneAsset.Runtime
{
    public class OneAssetPackage
    {
        private readonly string _packageName;
        private readonly IEncryptRule _encryptRule;
        private ILoader _loader;

        public OneAssetPackage(string packageName, IEncryptRule encryptRule)
        {
            _packageName = packageName;
            _encryptRule = encryptRule;
            InitializeLoader();
        }

        private void InitializeLoader()
        {
#if UNITY_EDITOR
            if (OneAssets.GetPlayMode() == EPlayMode.AssetBundle)
            {
                _loader = new AssetBundleLoader(_packageName, _encryptRule);
            }
            else
            {
                _loader = new AssetDatabaseLoader(_packageName, _encryptRule);
            }
#else
            _loader = new AssetBundleLoader(_packageName, _encryptRule);
#endif
        }

        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            return _loader.LoadAsset<T>(assetPath);
        }

        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete) where T : UnityEngine.Object
        {
            _loader.LoadAssetAsync<T>(assetPath, onComplete);
        }

        public void UnloadAsset(string assetPath)
        {
            _loader.UnloadAsset(assetPath);
        }
    }
}