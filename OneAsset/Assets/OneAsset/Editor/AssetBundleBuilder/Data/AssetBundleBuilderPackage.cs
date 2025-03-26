using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Data
{
    [Serializable]
    public class AssetBundleBuilderPackage
    {
        public string packageName;
        public string buildVersion = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        public EBuildMode buildMode = EBuildMode.ForceRebuild;
        public string encryptKey;
        public ECompressMode compressMode = ECompressMode.Default;
        public ENameMode nameMode = ENameMode.HashName;


        public BuildAssetBundleOptions GetBuildAssetBundleOptions()
        {
            switch (buildMode)
            {
                case EBuildMode.ForceRebuild:
                {
                    return BuildAssetBundleOptions.ForceRebuildAssetBundle;
                }
            }

            return BuildAssetBundleOptions.None;
        }

        public string GetOutputPath()
        {
            return Path.Combine(Application.dataPath, "../Bundles", packageName);
        }

        public BuildTarget GetBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget;
        }
    }

    public enum EBuildMode
    {
        ForceRebuild = 0,
    }

    public enum ECompressMode
    {
        Default = 0,
        LZ4 = 1,
    }

    public enum ENameMode
    {
        HashName = 0,
        BundleName = 1,
        BundleNameHashName = 2,
    }
}