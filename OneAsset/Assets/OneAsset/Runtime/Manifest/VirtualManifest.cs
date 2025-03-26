using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OneAsset.Runtime.Manifest
{
    [Serializable]
    public class VirtualManifest
    {
        public string version;
        public long time;
        public List<PackageInfo> packages = new List<PackageInfo>();

        private static VirtualManifest _default;

        public static VirtualManifest Default
        {
            get
            {
                if (_default != null) return _default;
                var json = File.ReadAllText(OneAssetSetting.ManifestPath);
                _default = JsonUtility.FromJson<VirtualManifest>(json);
                AssetBundles.Clear();
                foreach (var package in _default.packages)
                {
                    foreach (var group in package.groups)
                    {
                        foreach (var bundleAsset in group.bundles)
                        {
                            foreach (var assetInfo in bundleAsset.assets)
                            {
                                AssetBundles.Add(assetInfo.address, bundleAsset);
                            }
                        }
                    }
                }

                return _default;
            }
        }

        public void Save()
        {
            var path = OneAssetSetting.ManifestPath;

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = JsonUtility.ToJson(this);
            File.WriteAllText(path, json);
        }

        private static readonly Dictionary<string, BundleInfo> AssetBundles = new Dictionary<string, BundleInfo>();
        public string GetAssetPathByAddress(string address)
        {
            if (AssetBundles.TryGetValue(address, out var bundleInfo))
            {
                foreach (var assetInfo in bundleInfo.assets)
                {
                    if (assetInfo.address == address)
                        return assetInfo.assetPath;
                }
            }

            OneAssetLogger.LogWarning($"Can not get bundle name: {address}");
            return address;
        }
        
        public string GetBundleNameByAddress(string address)
        {
            if (AssetBundles.TryGetValue(address, out var bundleInfo))
            {
                return bundleInfo.name;
            }

            OneAssetLogger.LogError($"Can not get bundle name: {address}");
            return null;
        }

        public List<string> GetAllDependsBundleByAddress(string address)
        {
            if (AssetBundles.TryGetValue(address, out var bundleInfo))
            {
                return bundleInfo.depends;
            }

            OneAssetLogger.LogError($"Can not get bundle name: {address}");
            return null;
        }
    }

    [Serializable]
    public class PackageInfo
    {
        public string name;
        public List<GroupInfo> groups = new List<GroupInfo>();
    }

    [Serializable]
    public class GroupInfo
    {
        public string name;
        public List<BundleInfo> bundles = new List<BundleInfo>();
    }

    [Serializable]
    public class BundleInfo
    {
        public string name;
        public string hash;
        public List<AssetInfo> assets = new List<AssetInfo>();
        public List<string> depends = new List<string>();
    }

    [Serializable]
    public class AssetInfo
    {
        public string address;
        public string assetPath;
        public string assetGuid;
        public List<string> assetTags;
        public long bundleId;
    }
}