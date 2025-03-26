using System.Collections.Generic;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Editor.AssetBundleBuilder.Pipeline;
using OneAsset.Runtime;

namespace OneAsset.Editor.AssetBundleBuilder
{
    public static class AssetBundleBuilder
    {
        public static void Build(AssetBundleBuilderPackage builderPackage)
        {
            if (builderPackage == null)
                return;
            PipelineHandler.Run(new List<AssetBundleBuilderPackage>
            {
                builderPackage
            });
            OneAssetLogger.Log("BuildAssetBundles successful !");
        }
    }
}