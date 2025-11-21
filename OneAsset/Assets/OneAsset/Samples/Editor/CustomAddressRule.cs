using OneAsset.Editor.AssetBundleCollector.Rule;

namespace OneAsset.Samples.Editor
{
    public class AddressWithoutTopRoot : IAddressRule
    {
        private const string TopPath = "Assets/OneAsset/Samples/Runtime/";
        public string GetAddress(string groupName, string assetPath)
        {
            return assetPath.Replace(TopPath,string.Empty);
        }
    }
}