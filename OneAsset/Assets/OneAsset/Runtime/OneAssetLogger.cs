using UnityEngine;

namespace OneAsset.Runtime
{
    public static class OneAssetLogger
    {
        public static void Log(string msg)
        {
            Debug.Log($"[OneAsset] {msg}");
        }
        
        public static void LogWarning(string msg)
        {
            Debug.LogWarning($"[OneAsset] {msg}");
        }
        
        public static void LogError(string msg)
        {
            Debug.LogError($"[OneAsset] {msg}");
        }
    }
}