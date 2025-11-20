using System.Collections.Generic;
using System.Diagnostics;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Runtime;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public static class PipelineHandler
    {
        private static readonly List<IPipeline> Pipelines;

        static PipelineHandler()
        {
            Pipelines = new List<IPipeline>
            {
                new BeginPipeline(),
                new BuildBundlePipeline(),
                new BuildReportPipeline(),
                new EntryptBundlePipeline(),
                new EndPipeline(),
            };
        }

        public static void Run(IList<AssetBundleBuilderPackage> assetBundleBuilderPackages)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            foreach (var assetBundleBuilderPackage in assetBundleBuilderPackages)
            {
                var pipelineData = new PipelineData {AssetBundleBuilderPackage = assetBundleBuilderPackage};
                foreach (var pipeline in Pipelines)
                {
                    pipeline.Run(pipelineData);
                }
            }

            stopWatch.Stop();
            AssetDatabase.Refresh();
            OneAssetLogger.Log($"Build Pipeline Finish, TotalMinutes = {stopWatch.Elapsed.TotalMinutes}");
        }
    }
}