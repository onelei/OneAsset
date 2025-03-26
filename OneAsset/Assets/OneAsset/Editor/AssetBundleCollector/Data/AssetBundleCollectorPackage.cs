using System;
using System.Collections.Generic;

namespace OneAsset.Editor.AssetBundleCollector.Data
{
    [Serializable]
    public class AssetBundleCollectorPackage
    {
        public string packageName;
        public string packageDesc;
        public List<AssetBundleCollectorGroup> groups = new List<AssetBundleCollectorGroup>();
    }
}