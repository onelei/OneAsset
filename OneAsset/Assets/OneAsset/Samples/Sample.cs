using OneAsset.Runtime;
using UnityEngine;

namespace OneAsset.Samples
{
    public class Sample : MonoBehaviour
    {
        public void Start()
        {
            var assetPath = string.Empty;
            //Sync
            var syncAsset = OneAssets.LoadAsset<GameObject>(assetPath);
            Instantiate(syncAsset);
            
            //Async
            OneAssets.LoadAssetAsync<GameObject>(assetPath, (asset) =>
            {
                Instantiate(asset);
            });
            
            //Unload
            OneAssets.UnloadAsset(assetPath);
        }
    }
}