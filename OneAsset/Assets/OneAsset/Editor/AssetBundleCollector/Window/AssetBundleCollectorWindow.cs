using System;
using System.IO;
using System.Linq;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Editor.AssetBundleCollector.Data;
using OneAsset.Editor.Core;
using OneAsset.Runtime;
using OneAsset.Runtime.Core;
using OneAsset.Runtime.Manifest;
using UnityEditor;
using UnityEngine;
using PackageInfo = OneAsset.Runtime.Manifest.PackageInfo;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    public class AssetBundleCollectorWindow : BaseEditorWindow
    {
        [MenuItem("OneAsset/Editor/AssetBundleCollectorWindow")]
        public static void Open()
        {
            var window = GetWindow<AssetBundleCollectorWindow>("AssetBundle Collector", true);
            window.minSize = new Vector2(800, 600);
        }

        private AssetBundleCollectorSetting _setting;
        private bool _isFoldoutSetting = true;
        private Vector2 _scrollPosition;
        private AssetBundlePackageTreeView _packageTreeView;
        private AssetBundleGroupTreeView _groupTreeView;
        private AssetBundleDirectoryTreeView _directoryTreeView;

        protected override void OnInit()
        {
            base.OnInit();
            _setting = AssetBundleCollectorSetting.Default;
            _packageTreeView = new AssetBundlePackageTreeView
            {
                OnSelectedChange = (package) =>
                {
                    if (package != null)
                    {
                        _groupTreeView.SetData(package);
                    }
                }
            };
            _groupTreeView = new AssetBundleGroupTreeView
            {
                OnSelectedChange = (group) =>
                {
                    if (group != null)
                    {
                        _directoryTreeView.SetData(group);
                    }
                }
            };
            _directoryTreeView = new AssetBundleDirectoryTreeView();
            _packageTreeView?.SetData(_setting.packages);
            //Default
            if (_setting.packages.Count > 0)
            {
                _groupTreeView.SetData(_setting.packages[0]);
            }
        }

        protected override void OnUpdate()
        {
            //Header
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open"))
                {
                }

                if (GUILayout.Button("Save"))
                {
                    //Save 
                    Save();
                }
            }
            EditorGUILayout.EndHorizontal();
            //Settings
            if (GUILayout.Button("Settings"))
            {
                _isFoldoutSetting = !_isFoldoutSetting;
            }

            if (_isFoldoutSetting)
            {
                _setting.showPackage = GUILayout.Toggle(_setting.showPackage, "Show Package");
                _setting.showRuleAlias = GUILayout.Toggle(_setting.showRuleAlias, "Show Rule Alias");
                _setting.uniqueBundleName = GUILayout.Toggle(_setting.uniqueBundleName, "Unique Bundle Name");

                _setting.enableAddressable = GUILayout.Toggle(_setting.enableAddressable, "Enable Addressable");
                _setting.locationToLower = GUILayout.Toggle(_setting.locationToLower, "Location To Lower");
                _setting.includeAssetGuid = GUILayout.Toggle(_setting.includeAssetGuid, "Include Asset Guid");
                _setting.ignoreDefaultType = GUILayout.Toggle(_setting.ignoreDefaultType, "Ignore Default Type");
                _setting.autoCollectShaders = GUILayout.Toggle(_setting.autoCollectShaders, "Auto Collect Shaders");
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawHorizontalLine();
            EditorGUILayout.BeginHorizontal();
            {
                //Package
                if (_setting.showPackage)
                {
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
                }

                //Groups
                DrawVerticalLine();
                EditorGUILayout.BeginVertical(GUILayout.Width(150));
                {
                    EditorGUILayout.LabelField("Groups");
                    DrawHorizontalLine();
                    var treeViewRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    {
                        _groupTreeView.OnGUI(treeViewRect);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                //Directory
                DrawVerticalLine();
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField("Directories");
                    DrawHorizontalLine();
                    var treeViewRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    {
                        _directoryTreeView.OnGUI(treeViewRect);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
 
        private void Save()
        {
            //Save 
            EditorUtility.SetDirty(_setting);
            AssetDatabase.SaveAssets();
            //Save Builder
            var builderSetting = ScriptableObjectLoader.Load<AssetBundleBuilderSetting>();
            var packageNames = ListPool<string>.Get();
            foreach (var package in _setting.packages)
            {
                packageNames.Add(package.packageName);
            }

            builderSetting.Refresh(packageNames);
            ListPool<string>.Release(packageNames);
            EditorUtility.SetDirty(builderSetting);
            AssetDatabase.SaveAssets();
            //Save runtime
            var manifest = new VirtualManifest {time = DateTime.Now.Ticks};
            foreach (var package in _setting.packages)
            {
                var packageInfo = new PackageInfo {name = package.packageName};
                foreach (var group in package.groups)
                {
                    if (!group.IsValid())
                        continue;
                    var groupInfo = new GroupInfo {name = group.groupName};
                    foreach (var directory in group.directories)
                    {
                        if (!directory.IsValid())
                            continue;
                        var bundleInfo = new BundleInfo {name = directory.GetBundleName()};
                        bundleInfo.assets.Clear();
                        var assetPaths = directory.GetMainAssets();
                        foreach (var assetPath in assetPaths)
                        {
                            var addressRule = directory.GetAddressRule();
                            var assetInfo = new AssetInfo
                            {
                                assetPath = assetPath,
                                assetGuid = AssetDatabase.AssetPathToGUID(assetPath),
                                assetTags = directory.tags.Split(',').ToList(),
                                bundleId = 0,
                                address = addressRule?.GetAddress(group.groupName, assetPath),
                            };
                            bundleInfo.assets.Add(assetInfo);
                        }

                        groupInfo.bundles.Add(bundleInfo);
                    }

                    packageInfo.groups.Add(groupInfo);
                }

                manifest.packages.Add(packageInfo);
            }

            var outputPath = OneAssetSetting.GetManifestPath();
            try
            {
                File.WriteAllText(outputPath, JsonUtility.ToJson(manifest, true));
            }
            catch (Exception e)
            {
                OneAssetLogger.LogError(e.Message);
            }
            finally
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                OneAssetLogger.Log($"Save Successful: {outputPath}");
            }
        }
    }
}