using OneAsset.Runtime.Manifest;

namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public class BeginPipeline: IPipeline
    {
        public void Run(PipelineData pipelineData)
        {
            //Prepare CustomManifest
            pipelineData.CustomVirtualManifest = VirtualManifest.Default;
        }
    }
}