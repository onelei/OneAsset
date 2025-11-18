using OneAsset.Runtime;
using UnityEngine;

namespace OneAsset.Samples
{
    public class Sample : MonoBehaviour
    {
        public EPlayMode playMode;

        public void Start()
        {
            //Init
            OneAssets.Init(playMode);
            
            //Sync
            var assetPath = "Prefabs/UI_Sample.prefab";
            var prefabAsset = OneAssets.LoadAsset<GameObject>(assetPath);
            var uiSample = Instantiate(prefabAsset, transform, false);
            uiSample.name = "UISample";
            var spritePath = "Sprites/UI/UISample/Emoji_Aristocrat.png";
            var spriteAsset = OneAssets.LoadAsset<Sprite>(spritePath);
            uiSample.GetComponent<UISample>().SetIcon(spriteAsset);

            //Async
            OneAssets.LoadAssetAsync<GameObject>(assetPath, (prefabAssetAsync) =>
            {
                uiSample = Instantiate(prefabAssetAsync, transform, false);
                uiSample.name = "UISample_Async";
                OneAssets.LoadAssetAsync<Sprite>(spritePath,
                    (spriteAssetAsync) => { uiSample.GetComponent<UISample>().SetIcon(spriteAssetAsync); });
            });
            //Unload
            // OneAssets.UnloadAsset(assetPath);
            // OneAssets.UnloadAsset(spritePath);
        }
    }
}