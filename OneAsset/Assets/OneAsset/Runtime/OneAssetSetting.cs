using System.IO;
using UnityEngine;

namespace OneAsset.Runtime
{
    public static class OneAssetSetting
    {
        public static string GetManifestPath()
        {
            return Path.Combine(GetAssetBundlesRootPath(), "OneAssetManifest.json");
        }

        public static string GetAssetBundlesRootPath()
        {
            return Path.Combine(Application.dataPath, $"../Bundles/{GetPlatformFolderForAssetBundles()}/Output");
        }

        public static string GetAssetBundlesOriginPath()
        {
            return $"{GetAssetBundlesRootPath()}/../Origin";
        }

        public static string GetAssetBundlesOutputPath()
        {
            return Path.Combine(Application.dataPath, $"../Bundles/{GetPlatformFolderForAssetBundles()}/Output");
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