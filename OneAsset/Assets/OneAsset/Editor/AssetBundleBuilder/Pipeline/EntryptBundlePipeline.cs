using System.IO;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public class EntryptBundlePipeline : IPipeline
    {
        public void Run(PipelineData pipelineData)
        {
            var manifestInfo = pipelineData.CustomVirtualManifest;
            var builderPackage = pipelineData.AssetBundleBuilderPackage;
            var packageName = builderPackage.packageName;
            var outputPath = builderPackage.GetOutputPath();
            var outputEncryptPath = builderPackage.GetOutputEncryptPath();
            if (!Directory.Exists(outputEncryptPath))
            {
                Directory.CreateDirectory(outputEncryptPath);
            }

            foreach (var package in manifestInfo.packages)
            {
                if (package.name == packageName)
                {
                    foreach (var group in package.groups)
                    {
                        foreach (var bundleAsset in group.bundles)
                        {
                            // Read
                            var assetBundleName = bundleAsset.name;
                            var path = Path.Combine(outputPath, assetBundleName);
                            var allBytes = File.ReadAllBytes(path);
                            if (builderPackage.IsEntryptable())
                            {
                                allBytes = builderPackage.GetEntryptRule().Encrypt(allBytes);
                            }

                            // Write
                            path = Path.Combine(outputEncryptPath, assetBundleName);
                            File.WriteAllBytes(path, allBytes);
                        }
                    }
                }
            }
        }
    }
}