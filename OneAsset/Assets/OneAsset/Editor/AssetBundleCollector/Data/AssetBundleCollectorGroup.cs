using System;
using System.Collections.Generic;

namespace OneAsset.Editor.AssetBundleCollector.Data
{
    public enum EGroupActive
    {
        Enable = 0,
        Disable = 1,
    }

    [Serializable]
    public class AssetBundleCollectorGroup
    {
        public EGroupActive groupActive = EGroupActive.Enable;
        public string groupName;
        public string groupDesc;
        public string tags;
        public List<AssetBundleDirectory> directories = new List<AssetBundleDirectory>();

        public bool IsValid()
        {
            return groupActive == EGroupActive.Enable;
        }
    }
}