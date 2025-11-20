using System;
using System.Collections.Generic;
using System.IO;
using OneAsset.Editor.AssetBundleBuilder.Rule;
using OneAsset.Runtime;
using OneAsset.Runtime.Rule;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Data
{
    [Serializable]
    public class AssetBundleBuilderPackage
    {
        public string packageName;
        public string buildVersion = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        public EBuildMode buildMode = EBuildMode.ForceRebuild;
        public string encryptRule = nameof(OffsetEntryptRule);
        public ECompressMode compressMode = ECompressMode.LZ4;
        public ENameMode nameMode = ENameMode.HashName;


        public BuildAssetBundleOptions GetBuildAssetBundleOptions()
        {
            var options = BuildAssetBundleOptions.None;
            options |= BuildAssetBundleOptions.StrictMode;
            switch (buildMode)
            {
                case EBuildMode.ForceRebuild:
                {
                    options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                    break;
                }
                case EBuildMode.IncrementalBuild:
                {
                    options |= BuildAssetBundleOptions.IgnoreTypeTreeChanges;
                    break;
                }
            }

            switch (compressMode)
            {
                case ECompressMode.Uncompressed:
                {
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                    break;
                }
                case ECompressMode.LZ4:
                {
                    options |= BuildAssetBundleOptions.ChunkBasedCompression;
                    break;
                }
            }

            return options;
        }

        public string GetOutputPath()
        {
            return Path.Combine(OneAssetSetting.GetAssetBundlesRootPath(), "Origin", packageName);
        }

        public string GetOutputEncryptPath()
        {
            return Path.Combine(OneAssetSetting.GetAssetBundlesRootPath(), "Encrypt", packageName);
        }

        public BuildTarget GetBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }

        public string GetDefaultBuildVersion()
        {
            return DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        }

        public bool IsEntryptable()
        {
            return encryptRule != nameof(EntryptDisable);
        }

        private static readonly Dictionary<string, IEntryptRule> EntryptRules = new Dictionary<string, IEntryptRule>();

        public IEntryptRule GetEntryptRule()
        {
            if (!string.IsNullOrEmpty(encryptRule))
            {
                if (!EntryptRules.TryGetValue(encryptRule, out var entryptRule))
                {
                    var ruleType = RuleUtility.GetEntryptRuleType(encryptRule);
                    if (ruleType != null)
                    {
                        entryptRule = (IEntryptRule) Activator.CreateInstance(ruleType);
                        EntryptRules.Add(encryptRule, entryptRule);
                    }
                }

                return entryptRule;
            }

            return null;
        }
    }

    public enum EBuildMode
    {
        ForceRebuild = 0,
        IncrementalBuild = 1,
    }

    public enum ECompressMode
    {
        Uncompressed = 0,
        LZ4 = 1,
    }

    public enum ENameMode
    {
        HashName = 0,
        BundleName = 1,
        BundleNameHashName = 2,
    }
}