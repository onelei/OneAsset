using System.IO;
using OneAsset.Editor.AssetBundleCollector.Data;
using OneAsset.Runtime;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public class BuildBundlePipeline : IPipeline
    {
        public void Run(PipelineData pipelineData)
        {
            var builderPackage = pipelineData.AssetBundleBuilderPackage;
            if (builderPackage == null)
                return;
            var packageName = builderPackage.packageName;
            var outputPath = builderPackage.GetOutputPath();
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var assetBundleBuilds = AssetBundleCollectorSetting.Default.GetAssetBundleBuilds(packageName);
            var buildAssetBundleOptions = builderPackage.GetBuildAssetBundleOptions();
            var buildTarget = builderPackage.GetBuildTarget();
            var unityManifest =
                BuildPipeline.BuildAssetBundles(outputPath, assetBundleBuilds, buildAssetBundleOptions, buildTarget);
            if (unityManifest == null)
            {
                OneAssetLogger.LogError("BuildAssetBundles failed !");
                return;
            }

            pipelineData.Manifest = unityManifest;
            //Record depends
            var manifestInfo = pipelineData.CustomVirtualManifest;
            foreach (var package in manifestInfo.packages)
            {
                foreach (var group in package.groups)
                {
                    foreach (var bundleAsset in group.bundles)
                    {
                        var assetBundleName = bundleAsset.name;
                        bundleAsset.hash = unityManifest.GetAssetBundleHash(assetBundleName).ToString();
                        bundleAsset.depends.Clear();
                        var depends = unityManifest.GetAllDependencies(assetBundleName);
                        bundleAsset.depends.AddRange(depends);
                    }
                }
            }
        }
    }
}