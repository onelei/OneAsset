using System.IO;
using UnityEngine;

namespace OneAsset.Runtime
{
    public static class OneAssetSetting
    {
        public static readonly string ManifestPath = Path.Combine(Application.dataPath, "OneAssetManifest.json");

        public static string GetAssetBundlesRootPath()
        {
            return Path.Combine(Application.dataPath, $"../Bundles/{GetPlatformFolderForAssetBundles()}");
        }

        public static string GetPlatformFolderForAssetBundles()
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