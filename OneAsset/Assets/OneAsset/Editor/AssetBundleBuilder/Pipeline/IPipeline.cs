using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Runtime.Manifest;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public class PipelineData
    {
        public AssetBundleBuilderPackage AssetBundleBuilderPackage;
        public AssetBundleManifest Manifest;
        public VirtualManifest CustomVirtualManifest;
    }
    
    public interface IPipeline
    {
        void Run(PipelineData pipelineData);
    }
}