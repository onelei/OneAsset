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
                var json = File.ReadAllText(OneAssetSetting.GetManifestPath());
                _default = JsonUtility.FromJson<VirtualManifest>(json);
                AddressToBundleInfos.Clear();
                BundleToBundleInfos.Clear();
                foreach (var package in _default.packages)
                {
                    foreach (var group in package.groups)
                    {
                        foreach (var bundleAsset in group.bundles)
                        {
                            foreach (var assetInfo in bundleAsset.assets)
                            {
                                bundleAsset.PackageName = package.name;
                                AddressToBundleInfos.Add(assetInfo.address, bundleAsset);
                            }

                            BundleToBundleInfos.Add(bundleAsset.name, bundleAsset);
                        }
                    }
                }

                return _default;
            }
        }

        public void Save()
        {
            var path = OneAssetSetting.GetManifestPath();

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(path, json);
        }

        private static readonly Dictionary<string, BundleInfo> AddressToBundleInfos =
            new Dictionary<string, BundleInfo>();

        private static readonly Dictionary<string, BundleInfo> BundleToBundleInfos =
            new Dictionary<string, BundleInfo>();

        public bool TryGetBundleInfoByAddress(string address, out BundleInfo bundleInfo) =>
            AddressToBundleInfos.TryGetValue(address, out bundleInfo);

        public bool TryGetBundleInfoByBundleName(string bundleName, out BundleInfo bundleInfo) =>
            BundleToBundleInfos.TryGetValue(bundleName, out bundleInfo);

        public bool TryGetEncryptRule(string packageName, out string encryptRule)
        {
            foreach (var packageInfo in packages)
            {
                if (packageInfo.name == packageName)
                {
                    encryptRule = packageInfo.encryptRule;
                    return true;
                }
            }

            encryptRule = null;
            return false;
        }

        public string GetAssetPathByAddress(string address)
        {
            if (AddressToBundleInfos.TryGetValue(address, out var bundleInfo))
            {
                return bundleInfo.GetAssetPath(address);
            }

            OneAssetLogger.LogWarning($"Can not get bundle name: {address}");
            return address;
        }

        public string GetBundleNameByAddress(string address)
        {
            if (AddressToBundleInfos.TryGetValue(address, out var bundleInfo))
            {
                return bundleInfo.name;
            }

            OneAssetLogger.LogError($"Can not get bundle name: {address}");
            return null;
        }

        public List<string> GetAllDependsBundleByAddress(string address)
        {
            if (AddressToBundleInfos.TryGetValue(address, out var bundleInfo))
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
        public string encryptRule;
        public string compressMode;
        public List<GroupInfo> groups = new List<GroupInfo>();
    }

    [Serializable]
    public class GroupInfo
    {
        public string name;
        public string language;
        public List<BundleInfo> bundles = new List<BundleInfo>();
    }

    [Serializable]
    public class BundleInfo
    {
        [NonSerialized] public string PackageName;
        public string name;
        public string hash;
        public uint crc;
        public List<AssetInfo> assets = new List<AssetInfo>();
        public List<string> depends = new List<string>();

        public string GetAssetPath(string address)
        {
            if (string.IsNullOrEmpty(address))
                return string.Empty;
            foreach (var assetInfo in assets)
            {
                if (assetInfo.address == address)
                    return assetInfo.assetPath;
            }

            OneAssetLogger.LogWarning($"Can not get bundle name: {address}");
            return address;
        }
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