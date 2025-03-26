using System.Collections.Generic;
using OneAsset.Editor.Core;
using OneAsset.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Data
{
    [CreateAssetMenu(fileName = "AssetBundleCollectorSetting",
        menuName = "OneAsset/Create AssetBundle Collector Setting", order = 0)]
    public class AssetBundleCollectorSetting : ScriptableObject
    {
        //Global Settings
        public bool showPackage = true;
        public bool showRuleAlias = false;
        public bool uniqueBundleName = true;

        public bool enableAddressable = true;
        public bool locationToLower = false;
        public bool includeAssetGuid = false;
        public bool ignoreDefaultType = true;
        public bool autoCollectShaders = true;

        public List<AssetBundleCollectorPackage> packages = new List<AssetBundleCollectorPackage>();

        private static AssetBundleCollectorSetting _default;

        public static AssetBundleCollectorSetting Default
        {
            get
            {
                if (_default == null)
                {
                    _default = ScriptableObjectLoader.Load<AssetBundleCollectorSetting>();
                }

                return _default;
            }
        }

        public bool TryGetPackage(string packageName, out AssetBundleCollectorPackage result)
        {
            foreach (var package in packages)
            {
                if (package.packageName == packageName)
                {
                    result = package;
                    return true;
                }
            }

            result = null;
            return false;
        }
        
        public AssetBundleBuild[] GetAssetBundleBuilds(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;
            var assetBundleBuilds = ListPool<AssetBundleBuild>.Get();
            if (Default.TryGetPackage(packageName, out var package))
            {
                foreach (var group in package.groups)
                {
                    foreach (var directory in group.directories)
                    {
                        var assetBundleBuild = new AssetBundleBuild
                        {
                            assetBundleName = directory.GetBundleName(),
                            assetBundleVariant = string.Empty,
                            assetNames = directory.GetMainAssets().ToArray()
                        };
                        assetBundleBuilds.Add(assetBundleBuild);
                    }
                }
            }

            var result = assetBundleBuilds.ToArray();
            ListPool<AssetBundleBuild>.Release(assetBundleBuilds);
            return result;
        }
    }
}