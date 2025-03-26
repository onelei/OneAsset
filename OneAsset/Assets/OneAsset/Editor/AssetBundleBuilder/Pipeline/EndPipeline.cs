namespace OneAsset.Editor.AssetBundleBuilder.Pipeline
{
    public class EndPipeline: IPipeline
    {
        public void Run(PipelineData pipelineData)
        {
            var manifestInfo = pipelineData.CustomVirtualManifest;
            manifestInfo.Save();
        }
    }
}