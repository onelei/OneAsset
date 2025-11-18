using System.IO;

namespace OneAsset.Editor.AssetBundleCollector.Rule
{
    public interface IAddressRule
    {
        string GetAddress(string groupName, string assetPath);
    }
    
    public class AddressDisable : IAddressRule
    {
        public string GetAddress(string groupName, string assetPath)
        {
            return null;
        }
    }
    
    public class AddressByFileName : IAddressRule
    {
        public string GetAddress(string groupName, string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath);
        }
    }
    
    public class AddressByFolderAndFileName : IAddressRule
    {
        public string GetAddress(string groupName, string assetPath)
        {
            var fileInfo = new FileInfo(assetPath);
            return fileInfo.Directory != null ? Path.Combine(fileInfo.Directory.Name, Path.GetFileNameWithoutExtension(assetPath)) : assetPath;
        }
    }
    
    public class AddressByGroupAndFileName : IAddressRule
    {
        public string GetAddress(string groupName, string assetPath)
        {
            return Path.Combine(groupName, Path.GetFileNameWithoutExtension(assetPath));
        }
    }
}