using System;
using System.Collections.Generic;
using System.IO;
using OneAsset.Editor.AssetBundleCollector.Rule;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleCollector.Data
{
    [Serializable]
    public class AssetBundleDirectory
    {
        public string path;
        public string collectorType = nameof(MainCollector);
        public string addressRuleType = nameof(AddressWithoutTopRoot);
        public string packRuleType = nameof(SmartPackDirectory);
        public string filterRuleType = nameof(CollectorAll);
        public string args = string.Empty;
        public string tags = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(path);
        }

        public bool IsAddressable()
        {
            return addressRuleType != nameof(AddressDisable);
        }

        private static readonly Dictionary<string, IAddressRule> AddressRules = new Dictionary<string, IAddressRule>();

        public IAddressRule GetAddressRule()
        {
            if (!string.IsNullOrEmpty(addressRuleType))
            {
                if (!AddressRules.TryGetValue(addressRuleType, out var addressRule))
                {
                    var ruleType = RuleUtility.GetAddressRuleType(addressRuleType);
                    if (ruleType != null)
                    {
                        addressRule = (IAddressRule) Activator.CreateInstance(ruleType);
                        AddressRules.Add(addressRuleType, addressRule);
                    }
                }

                return addressRule;
            }

            return null;
        }

        public string GetBundleName()
        {
            if (!string.IsNullOrEmpty(path))
            {
                var bundleName = path.Replace('/', '_').Replace('.', '_').ToLower();
                return bundleName;
            }

            return null;
        }

        private List<string> _assets = new List<string>();

        public List<string> GetMainAssets()
        {
            _assets.Clear();
            if (!string.IsNullOrEmpty(path))
            {
                var filePaths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var filePath in filePaths)
                {
                    if (IsValidAsset(filePath))
                    {
                        var fixPath = filePath.Replace("\\", "/");
                        _assets.Add(fixPath);
                    }
                }
            }

            return _assets;
        }

        private readonly HashSet<string> _ignoreFileExtensions = new HashSet<string>()
            {"", ".so", ".dll", ".cs", ".js", ".boo", ".meta", ".cginc", ".hlsl"};

        private bool IsValidAsset(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/") && !assetPath.StartsWith("Packages"))
                return false;
            if (AssetDatabase.IsValidFolder(assetPath))
                return false;
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (assetType == typeof(LightingDataAsset))
                return false;
            var extension = Path.GetExtension(assetPath);
            if (_ignoreFileExtensions.Contains(extension))
                return false;
            return true;
        }
    }
}