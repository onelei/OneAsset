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
        public string encryptRule = nameof(OffsetEncryptRule);
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

        public string GetOriginOutputPath()
        {
            return Path.Combine(OneAssetSetting.GetAssetBundlesOriginPath(), packageName);
        }

        public string GetFinalOutputPath()
        {
            return Path.Combine(OneAssetSetting.GetAssetBundlesOutputPath(), packageName);
        }

        public BuildTarget GetBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }

        public string GetDefaultBuildVersion()
        {
            return DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        }

        public bool IsEncryptable()
        {
            return encryptRule != nameof(EncryptDisable);
        }

        private static readonly Dictionary<string, IEncryptRule> EncryptRules = new Dictionary<string, IEncryptRule>();
        
        public string GetEncryptRuleTypeName()
        {
            return GetEncryptRule().GetType().FullName;
        }

        public IEncryptRule GetEncryptRule()
        {
            if (!string.IsNullOrEmpty(encryptRule))
            {
                if (!EncryptRules.TryGetValue(encryptRule, out var encryptInsRule))
                {
                    var ruleType = RuleUtility.GetEncryptRuleType(encryptRule);
                    if (ruleType != null)
                    {
                        encryptInsRule = (IEncryptRule) Activator.CreateInstance(ruleType);
                        EncryptRules.Add(encryptRule, encryptInsRule);
                    }
                }

                return encryptInsRule;
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