using OneAsset.Runtime;
using System.Collections;
using OneAsset.Runtime.Rule;
using UnityEngine;

namespace OneAsset.Samples
{
    public class Sample : MonoBehaviour
    {
        public EPlayMode playMode;
        private string assetPath = "Prefabs/UI_Sample.prefab";
        private string spritePath = "Sprites/UI/UISample/Emoji_Aristocrat.png";
        private GameObject _uiSample;
        private readonly WaitForSeconds _waitForSeconds = new WaitForSeconds(5);
        
        void Awake()
        {
            Application.lowMemory += OnLowMemory;
        }

        void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }

        public void Start()
        {
            //Init
            OneAssets.SetPlayMode(playMode);
            OneAssets.SetPackage(new OneAssetPackage("Bundles", new OffsetEncryptRule()));
            //Sync
            var prefabAsset = OneAssets.LoadAsset<GameObject>(assetPath);
            _uiSample = Instantiate(prefabAsset, transform, false);
            _uiSample.name = "UISample";
            var spriteAsset = OneAssets.LoadAsset<Sprite>(spritePath);
            _uiSample.GetComponent<UISample>().SetIcon(spriteAsset);

            //Delayed 5 seconds
            StartCoroutine(LoadAssetDelayed());

            //Unload
            // OneAssets.UnloadAsset(assetPath);
            // OneAssets.UnloadAsset(spritePath);
        }

        private IEnumerator LoadAssetDelayed()
        {
            yield return _waitForSeconds;

            //Async
            OneAssets.LoadAssetAsync<GameObject>(assetPath, (prefabAssetAsync) =>
            {
                _uiSample = Instantiate(prefabAssetAsync, transform, false);
                _uiSample.name = "UISample_Async";
                OneAssets.LoadAssetAsync<Sprite>(spritePath,
                    (spriteAssetAsync) => { _uiSample.GetComponent<UISample>().SetIcon(spriteAssetAsync); });
            });
        }
        
        private void OnLowMemory()
        {
            OneAssets.UnloadUnusedBundles(true);
        }
        
        private void Update()
        { 
            OneAssets.UnloadUnusedBundles();
        }
    }
}