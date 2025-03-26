using System.IO;
using UnityEngine;

namespace OneAsset.Runtime
{
    public static class OneAssetSetting
    {
        public static readonly string ManifestPath = Path.Combine(Application.dataPath, "OneAssetManifest.json");
    }
}