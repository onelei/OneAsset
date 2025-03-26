using System.IO;

namespace OneAsset.Editor.AssetBundleCollector.Rule
{
    public interface IPackRule
    {
        string GetAddress(string packageName, string groupName, string assetPath);
    }
    
    public class SmartPackDirectory : IPackRule
    {
        public string GetAddress(string packageName, string groupName, string assetPath)
        {
            return null;
        }
    }
}