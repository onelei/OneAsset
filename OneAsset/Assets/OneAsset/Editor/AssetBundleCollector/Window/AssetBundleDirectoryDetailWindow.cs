using OneAsset.Editor.AssetBundleCollector.Data;
using OneAsset.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    public class AssetBundleDirectoryDetailWindow : BaseEditorWindow
    {
        public static void Open(string groupName, AssetBundleDirectory directory)
        {
            _groupName = groupName;
            _directory = directory;
            var window = GetWindow<AssetBundleDirectoryDetailWindow>("Directory Detail", true);
            window.minSize = new Vector2(600, 600);
            _isInit = false;
        }

        private static bool _isInit = false;
        private static string _groupName;
        private static AssetBundleDirectory _directory;
        private Vector2 _scrollPosition;
        private AssetBundleDirectoryDetailTreeView _treeView;

        protected override void OnInit()
        {
            base.OnInit();
            _treeView = new AssetBundleDirectoryDetailTreeView();
            _treeView.SetData(_groupName,_directory);
            _isInit = true;
        }

        protected override void OnUpdate()
        {
            if (!_isInit)
            {
                OnInit();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawHorizontalLine();
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.LabelField("Details");
                DrawHorizontalLine();
                var treeViewRect =
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                {
                    _treeView.OnGUI(treeViewRect);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}