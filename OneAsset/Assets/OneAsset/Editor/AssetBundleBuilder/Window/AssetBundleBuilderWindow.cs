using System;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    public class AssetBundleBuilderWindow : BaseEditorWindow
    {
        [MenuItem("OneAsset/Editor/AssetBundleBuilderWindow")]
        public static void Open()
        {
            var window = GetWindow<AssetBundleBuilderWindow>("AssetBundle Builder", true);
            window.minSize = new Vector2(800, 600);
        }

        private AssetBundleBuilderSetting _setting;
        private Vector2 _scrollPosition;
        private AssetBundleBuilderPackageTreeView _packageTreeView;
        private AssetBundleBuilderPackage _package;
        private bool _isBuilding = false;
        protected override void OnInit()
        {
            base.OnInit();
            _setting = AssetBundleBuilderSetting.Default;
            _packageTreeView = new AssetBundleBuilderPackageTreeView
            {
                OnSelectedChange = (package) =>
                {
                    _package = package;
                    RefreshPackage();
                }
            };
            if (_setting.packages.Count > 0)
            {
                _package = _setting.packages[0];
            }
            RefreshPackage();
        }

        protected override void OnUpdate()
        { 
            EditorGUILayout.BeginHorizontal();
            {
                //Package
                EditorGUILayout.BeginVertical(GUILayout.Width(150));
                {
                    EditorGUILayout.LabelField("Packages");
                    DrawHorizontalLine();
                    var treeViewRect =
                        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    {
                        _packageTreeView.OnGUI(treeViewRect);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                //Settings  
                DrawVerticalLine();
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("Settings");
                    DrawHorizontalLine();
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        {
                            if (_package != null)
                            {
                                EditorGUILayout.TextField("Build Output", _package.GetOutputPath());
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        if (_package != null)
                        {
                            _package.buildVersion = EditorGUILayout.TextField("Build Version", _package.buildVersion);
                            _package.buildMode = (EBuildMode)EditorGUILayout.EnumPopup("Build Mode", _package.buildMode);
                            _package.encryptKey = EditorGUILayout.TextField("Encrypt Key", _package.encryptKey);
                            _package.compressMode = (ECompressMode)EditorGUILayout.EnumPopup("Compress Mode", _package.compressMode);
                            _package.nameMode = (ENameMode)EditorGUILayout.EnumPopup("Build Mode", _package.nameMode);
                            GUILayout.FlexibleSpace();
                            if(GUILayout.Button("Build AssetBundle",GUILayout.Height(50f)))
                            {
                                _isBuilding = true;
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            //Build
            if (_isBuilding)
            {
                _isBuilding = false;
                AssetBundleBuilder.Build(_package);
            }
        }

        private void RefreshPackage()
        {
            _packageTreeView?.SetData(_setting.packages);
        }
    }
}