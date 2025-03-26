namespace OneAsset.Runtime.Loader
{
    public static class LoaderHandler
    {
        private static readonly ILoader Loader;

        static LoaderHandler()
        {
#if UNITY_EDITOR
            if (OneAssets.GetPlayMode() == EPlayMode.AssetBundle)
            {
                Loader = new AssetBundleLoader();
            }
            else
            {
                Loader = new AssetDatabaseLoader();
            }
#else
            Loader = new AssetBundleLoader();
#endif
        }

        public static ILoader Default()
        {
            return Loader;
        }
    }
}