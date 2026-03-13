using System.Collections.Generic;

namespace OneAsset.Runtime
{
    public enum EPlayMode
    {
        Simulate,
        AssetBundle,
    }

    public static class OneAssets
    {
        private static EPlayMode _playMode = EPlayMode.Simulate;

        private static readonly Dictionary<string, OneAssetPackage>
            Packages = new Dictionary<string, OneAssetPackage>();

        public static void Initialize(EPlayMode playMode)
        {
            _playMode = playMode;
        }

        public static void AddPackage(OneAssetPackage package)
        {
            Packages[package.PackageName] = package;
        }

        public static OneAssetPackage GetPackage(string packageName)
        {
            return Packages[packageName];
        }

        public static bool TryGetPackage(string packageName, out OneAssetPackage package)
        {
            return Packages.TryGetValue(packageName, out package);
        }

        public static EPlayMode GetPlayMode() => _playMode;

        public static void UnloadUnusedBundles(bool immediate = false, bool unloadAllLoadedObjects = true)
        {
            using (var tor = Packages.GetEnumerator())
            {
                while (tor.MoveNext())
                {
                    var package = tor.Current.Value;
                    package.UnloadUnusedBundles(immediate, unloadAllLoadedObjects);
                }
            }
        }
    }
}