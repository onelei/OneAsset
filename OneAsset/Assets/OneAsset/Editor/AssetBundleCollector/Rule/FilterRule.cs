using System.IO;

namespace OneAsset.Editor.AssetBundleCollector.Rule
{
    public interface IFilterRule
    {
        string GetAddress(string packageName, string groupName, string assetPath);
    }
    
    public class CollectorAll : IFilterRule
    {
        public string GetAddress(string packageName, string groupName, string assetPath)
        {
            return null;
        }
    }
}