using System.Collections.Generic;
using OneAsset.Editor.Core;
using OneAsset.Runtime.Core;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Data
{
    [CreateAssetMenu(fileName = "AssetBundleBuilderSetting",
        menuName = "OneAsset/Create AssetBundle Builder Setting", order = 1)]
    public class AssetBundleBuilderSetting : ScriptableObject
    {
        public List<AssetBundleBuilderPackage> packages = new List<AssetBundleBuilderPackage>();

        private static AssetBundleBuilderSetting _default;

        public static AssetBundleBuilderSetting Default
        {
            get
            {
                if (_default == null)
                {
                    _default = ScriptableObjectLoader.Load<AssetBundleBuilderSetting>();
                }

                return _default;
            }
        }

        public void Refresh(IList<string> packageNames)
        {
            var oldPackages = ListPool<AssetBundleBuilderPackage>.Get();
            oldPackages.AddRange(packages);
            packages.Clear();
            foreach (var packageName in packageNames)
            {
                AssetBundleBuilderPackage package = null;
                foreach (var oldPackage in oldPackages)
                {
                    if (packageName != oldPackage.packageName) continue;
                    package = oldPackage;
                    break;
                }

                if (package == null)
                {
                    package = new AssetBundleBuilderPackage
                    {
                        packageName = packageName,
                    };
                }

                packages.Add(package);
            }

            ListPool<AssetBundleBuilderPackage>.Release(oldPackages);
        }
    }
}