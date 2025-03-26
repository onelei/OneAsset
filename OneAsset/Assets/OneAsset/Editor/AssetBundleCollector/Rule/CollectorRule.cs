using System.IO;

namespace OneAsset.Editor.AssetBundleCollector.Rule
{
    public interface ICollectorRule
    {
        string GetAddress(string packageName, string groupName, string assetPath);
    }
    
    public class MainCollector : ICollectorRule
    {
        public string GetAddress(string packageName, string groupName, string assetPath)
        {
            return null;
        }
    }
    
    public class DependenceCollector : ICollectorRule
    {
        public string GetAddress(string packageName, string groupName, string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}