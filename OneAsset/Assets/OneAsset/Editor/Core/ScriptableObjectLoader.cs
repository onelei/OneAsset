using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.Core
{
    public static class ScriptableObjectLoader
    {
        public static T Load<T>() where T : ScriptableObject
        {
            var scriptObjectType = typeof(T);
            var assetGuilds = AssetDatabase.FindAssets($"t:{scriptObjectType.Name}");
            if (assetGuilds.Length == 0)
            {
                Debug.LogWarning($"Create new {scriptObjectType.Name}.asset");
                var setting = ScriptableObject.CreateInstance<T>();
                string filePath = $"Assets/{scriptObjectType.Name}.asset";
                AssetDatabase.CreateAsset(setting, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return setting;
            }
            else
            {
                if (assetGuilds.Length != 1)
                {
                    foreach (var guid in assetGuilds)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var settings = AssetDatabase.LoadAssetAtPath<T>(path);
                        if (settings != null && settings.GetType() == scriptObjectType)
                        {
                            return settings;
                        }
                    }

                    throw new System.Exception($"Found multiple {scriptObjectType.Name} files !");
                }

                var filePath = AssetDatabase.GUIDToAssetPath(assetGuilds[0]);
                var setting = AssetDatabase.LoadAssetAtPath<T>(filePath);
                return setting;
            }
        }
    }
}