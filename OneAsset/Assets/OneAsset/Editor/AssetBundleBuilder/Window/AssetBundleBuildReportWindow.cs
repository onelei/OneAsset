using System.Collections.Generic;
using System.IO;
using OneAsset.Editor.AssetBundleBuilder.Data;
using OneAsset.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    /// <summary>
    /// Build report viewer window (using TreeView)
    /// </summary>
    public class AssetBundleBuildReportWindow : EditorWindow
    {
        private BuildReportData _currentReport;
        private Vector2 _scrollPosition;
        
        private AssetBundleBuildReportBundleTreeView _assetBundleBuildReportBundleTreeView;
        private TreeViewState _bundleTreeViewState;
        private SearchField _bundleSearchField;
        
        private AssetBundleBuildReportAssetTreeView _assetBundleBuildReportAssetTreeView;
        private TreeViewState _assetTreeViewState;
        private SearchField _assetSearchField;
        
        private AssetBundleBuildReportDependencyTreeView _assetBundleBuildReportDependencyTreeView;
        private TreeViewState _dependencyTreeViewState;
        private SearchField _dependencySearchField;
        
        private BundleReportInfo _selectedBundle;
        
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        
        private bool _showSummary = true;
        private bool _showInformation = true;
        
        [MenuItem("OneAsset/Editor/AssetBundleBuildReportWindow")]
        public static void OpenWindow()
        {
            var window = GetWindow<AssetBundleBuildReportWindow>("Build Report Viewer", true);
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        private void OnEnable()
        {
            InitTreeView();
            LoadLatestReport();
        }

        /// <summary>
        /// Initialize TreeView instances
        /// </summary>
        private void InitTreeView()
        {
            // Initialize bundle TreeView with multi-column header
            if (_bundleTreeViewState == null)
                _bundleTreeViewState = new TreeViewState();
            
            var bundleMultiColumnHeaderState = AssetBundleBuildReportBundleTreeView.CreateDefaultMultiColumnHeaderState();
            var bundleMultiColumnHeader = new MultiColumnHeader(bundleMultiColumnHeaderState);
            bundleMultiColumnHeader.ResizeToFit();
            
            _assetBundleBuildReportBundleTreeView = new AssetBundleBuildReportBundleTreeView(_bundleTreeViewState, bundleMultiColumnHeader);
            _assetBundleBuildReportBundleTreeView.OnBundleSelected += OnBundleSelected;
            
            _bundleSearchField = new SearchField();
            _bundleSearchField.downOrUpArrowKeyPressed += _assetBundleBuildReportBundleTreeView.SetFocusAndEnsureSelectedItem;
            
            // Initialize asset TreeView with multi-column header
            if (_assetTreeViewState == null)
                _assetTreeViewState = new TreeViewState();
            
            var assetMultiColumnHeaderState = AssetBundleBuildReportAssetTreeView.CreateDefaultMultiColumnHeaderState();
            var assetMultiColumnHeader = new MultiColumnHeader(assetMultiColumnHeaderState);
            assetMultiColumnHeader.ResizeToFit();
            
            _assetBundleBuildReportAssetTreeView = new AssetBundleBuildReportAssetTreeView(_assetTreeViewState, assetMultiColumnHeader);
            
            _assetSearchField = new SearchField();
            _assetSearchField.downOrUpArrowKeyPressed += _assetBundleBuildReportAssetTreeView.SetFocusAndEnsureSelectedItem;
            
            // Initialize dependency TreeView with multi-column header
            if (_dependencyTreeViewState == null)
                _dependencyTreeViewState = new TreeViewState();
            
            var dependencyMultiColumnHeaderState = AssetBundleBuildReportDependencyTreeView.CreateDefaultMultiColumnHeaderState();
            var dependencyMultiColumnHeader = new MultiColumnHeader(dependencyMultiColumnHeaderState);
            dependencyMultiColumnHeader.ResizeToFit();
            
            _assetBundleBuildReportDependencyTreeView = new AssetBundleBuildReportDependencyTreeView(_dependencyTreeViewState, dependencyMultiColumnHeader);
            
            _dependencySearchField = new SearchField();
            _dependencySearchField.downOrUpArrowKeyPressed += _assetBundleBuildReportDependencyTreeView.SetFocusAndEnsureSelectedItem;
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();

            if (_currentReport == null)
            {
                DrawNoReportMessage();
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                DrawSummarySection();
                
                EditorGUILayout.Space(10);
                
                DrawBuildInfoSection();
                
                EditorGUILayout.Space(10);
                
                DrawBundleTreeViewSection();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draw toolbar
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Load Latest", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    LoadLatestReport();
                }

                if (GUILayout.Button("Load Report...", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    LoadReportFromFile();
                }

                if (_currentReport != null)
                {
                    if (GUILayout.Button("Export to TXT", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    {
                        ExportReportToTxt();
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label($"Report Version: {_currentReport?.summary.reportVersion ?? "N/A"}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw no report message
        /// </summary>
        private void DrawNoReportMessage()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No Build Report Loaded", _headerStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Please click 'Load Latest' or 'Load Report...' button", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw summary section
        /// </summary>
        private void DrawSummarySection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                _showSummary = EditorGUILayout.BeginFoldoutHeaderGroup(_showSummary, "Summary");
                
                if (_showSummary)
                {
                    EditorGUILayout.Space(5);

                    var summary = _currentReport.summary;
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Total Bundles:", summary.totalBundleCount.ToString());
                    DrawLabelField("Total Assets:", summary.totalAssetCount.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Total Size:", summary.totalSizeReadable);
                    DrawLabelField("Largest Bundle:", summary.largestBundle);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Build Status:", summary.buildStatus);
                    DrawLabelField("Largest Bundle Size:", summary.largestBundleSizeReadable);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw build information section
        /// </summary>
        private void DrawBuildInfoSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                _showInformation = EditorGUILayout.BeginFoldoutHeaderGroup(_showInformation, "Information");
                
                if (_showInformation)
                {
                    EditorGUILayout.Space(5);

                    var summary = _currentReport.summary;

                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Package Name:", summary.packageName);
                    DrawLabelField("Build Time:", summary.buildTime);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Build Target:", summary.buildTarget);
                    DrawLabelField("Build Duration:", $"{summary.buildDuration:F2}s");
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Build Mode:", summary.buildMode);
                    DrawLabelField("Compress Mode:", summary.compressMode);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Encrypt Rule:", summary.encryptRule);
                    EditorGUILayout.EndHorizontal();
                    
                    DrawLabelField("Output Path:", summary.outputPath);
                }
                
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw Bundle TreeView section
        /// </summary>
        private void DrawBundleTreeViewSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Bundle List", _headerStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                {
                    // Left: Bundle TreeView
                    EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));
                    {
                        // Bundle search box
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        {
                            var searchString = _bundleSearchField.OnToolbarGUI(_assetBundleBuildReportBundleTreeView.searchString);
                            _assetBundleBuildReportBundleTreeView.searchString = searchString;
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // Bundle TreeView
                        var rect = GUILayoutUtility.GetRect(0, 400, GUILayout.ExpandWidth(true));
                        _assetBundleBuildReportBundleTreeView.OnGUI(rect);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(10);

                    // Right: Asset TreeView and details
                    EditorGUILayout.BeginVertical();
                    {
                        if (_selectedBundle != null)
                        {
                            DrawBundleDetails(_selectedBundle);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Please select a Bundle to view details", MessageType.Info);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw Bundle details
        /// </summary>
        private void DrawBundleDetails(BundleReportInfo bundle)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                GUILayout.Label("Bundle Details", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                DrawLabelField("Name:", bundle.bundleName);
                DrawLabelField("Hash:", bundle.bundleHash);
                DrawLabelField("Size:", $"{bundle.bundleSizeReadable} ({bundle.bundleSize} bytes)");
                DrawLabelField("Asset Count:", bundle.assetCount.ToString());
                DrawLabelField("Dependencies:", bundle.dependencies.Count.ToString());
                
                EditorGUILayout.Space(5);
                
                // Dependencies TreeView
                if (bundle.dependencies.Count > 0)
                {
                    EditorGUILayout.LabelField("Dependencies List:", EditorStyles.boldLabel);
                    
                    // Dependency search box
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        var dependencySearchString = _dependencySearchField.OnToolbarGUI(_assetBundleBuildReportDependencyTreeView.searchString);
                        _assetBundleBuildReportDependencyTreeView.searchString = dependencySearchString;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    var dependencyRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
                    _assetBundleBuildReportDependencyTreeView.OnGUI(dependencyRect);
                }
                
                EditorGUILayout.Space(5);
                
                // Assets TreeView
                if (bundle.assets.Count > 0)
                {
                    EditorGUILayout.LabelField("Assets List:", EditorStyles.boldLabel);
                    
                    // Asset search box
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        var assetSearchString = _assetSearchField.OnToolbarGUI(_assetBundleBuildReportAssetTreeView.searchString);
                        _assetBundleBuildReportAssetTreeView.searchString = assetSearchString;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    var rect = GUILayoutUtility.GetRect(0, 300, GUILayout.ExpandWidth(true));
                    _assetBundleBuildReportAssetTreeView.OnGUI(rect);
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw label field
        /// </summary>
        private void DrawLabelField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Bundle selection callback
        /// </summary>
        private void OnBundleSelected(BundleReportInfo bundle)
        {
            _selectedBundle = bundle;
            _assetBundleBuildReportAssetTreeView.SetAssets(bundle?.assets);
            _assetBundleBuildReportDependencyTreeView.SetDependencies(bundle?.dependencies);
            Repaint();
        }

        /// <summary>
        /// Load latest report
        /// </summary>
        private void LoadLatestReport()
        {
            var assetBundlesPath = Path.Combine(OneAssetSetting.GetAssetBundlesRootPath(), "Origin");
            if (!Directory.Exists(assetBundlesPath))
            {
                EditorUtility.DisplayDialog("Error", "AssetBundles directory does not exist", "OK");
                return;
            }

            // Find all package directories
            var directories = Directory.GetDirectories(assetBundlesPath);
            string latestReportPath = null;

            foreach (var dir in directories)
            {
                var reportPath = Path.Combine(dir, "BuildReport_Latest.json");
                if (File.Exists(reportPath))
                {
                    latestReportPath = reportPath;
                    break;
                }
            }

            if (latestReportPath != null)
            {
                _currentReport = BuildReportData.LoadReport(latestReportPath);
                _assetBundleBuildReportBundleTreeView.SetBundles(_currentReport?.bundles);
                _selectedBundle = null;
                _assetBundleBuildReportAssetTreeView.SetAssets(null);
                _assetBundleBuildReportDependencyTreeView.SetDependencies(null);
                Repaint();
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "Build report not found, please build first", "OK");
            }
        }

        /// <summary>
        /// Load report from file
        /// </summary>
        private void LoadReportFromFile()
        {
            var path = EditorUtility.OpenFilePanel("Select Build Report", 
                OneAssetSetting.GetAssetBundlesRootPath(), "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                _currentReport = BuildReportData.LoadReport(path);
                _assetBundleBuildReportBundleTreeView.SetBundles(_currentReport?.bundles);
                _selectedBundle = null;
                _assetBundleBuildReportAssetTreeView.SetAssets(null);
                _assetBundleBuildReportDependencyTreeView.SetDependencies(null);
                Repaint();
            }
        }

        /// <summary>
        /// Export report to TXT
        /// </summary>
        private void ExportReportToTxt()
        {
            var path = EditorUtility.SaveFilePanel("Export Build Report", 
                OneAssetSetting.GetAssetBundlesRootPath(), 
                $"BuildReport_{_currentReport.summary.packageName}.txt", "txt");
            
            if (!string.IsNullOrEmpty(path))
            {
                ExportReportToTextFile(path);
            }
        }

        /// <summary>
        /// Export report to text file
        /// </summary>
        private void ExportReportToTextFile(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    var summary = _currentReport.summary;
                    
                    writer.WriteLine("========================================");
                    writer.WriteLine("        OneAsset Build Report");
                    writer.WriteLine("========================================");
                    writer.WriteLine();
                    
                    writer.WriteLine("=== Build Summary ===");
                    writer.WriteLine($"Total Bundles: {summary.totalBundleCount}");
                    writer.WriteLine($"Total Assets: {summary.totalAssetCount}");
                    writer.WriteLine($"Total Size: {summary.totalSizeReadable}");
                    writer.WriteLine($"Largest Bundle: {summary.largestBundle}");
                    writer.WriteLine($"Largest Bundle Size: {summary.largestBundleSizeReadable}");
                    writer.WriteLine($"Build Status: {summary.buildStatus}");
                    writer.WriteLine();
                    
                    writer.WriteLine("=== Build Information ===");
                    writer.WriteLine($"Package Name: {summary.packageName}");
                    writer.WriteLine($"Build Time: {summary.buildTime}");
                    writer.WriteLine($"Build Duration: {summary.buildDuration:F2}s");
                    writer.WriteLine($"Build Target: {summary.buildTarget}");
                    writer.WriteLine($"Build Mode: {summary.buildMode}");
                    writer.WriteLine($"Compress Mode: {summary.compressMode}");
                    writer.WriteLine($"Encrypt Rule: {summary.encryptRule}");
                    writer.WriteLine($"Output Path: {summary.outputPath}");
                    writer.WriteLine();
                    
                    writer.WriteLine("=== Bundle List ===");
                    foreach (var bundle in _currentReport.bundles)
                    {
                        writer.WriteLine();
                        writer.WriteLine($"Bundle: {bundle.bundleName}");
                        writer.WriteLine($"  Hash: {bundle.bundleHash}");
                        writer.WriteLine($"  Size: {bundle.bundleSizeReadable} ({bundle.bundleSize} bytes)");
                        writer.WriteLine($"  Asset Count: {bundle.assetCount}");
                        
                        if (bundle.dependencies.Count > 0)
                        {
                            writer.WriteLine($"  Dependencies:");
                            foreach (var dep in bundle.dependencies)
                            {
                                writer.WriteLine($"    - {dep}");
                            }
                        }
                        
                        writer.WriteLine($"  Assets:");
                        foreach (var asset in bundle.assets)
                        {
                            writer.WriteLine($"    - {asset.address}");
                            writer.WriteLine($"      Path: {asset.assetPath}");
                        }
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine("========================================");
                }
                
                EditorUtility.DisplayDialog("Success", $"Report exported to:\n{filePath}", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Export failed:\n{e.Message}", "OK");
            }
        }
    } 
}

