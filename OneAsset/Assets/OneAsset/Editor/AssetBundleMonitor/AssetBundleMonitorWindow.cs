using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleMonitor
{
    /// <summary>
    /// AssetBundle load monitor window
    /// </summary>
    public class AssetBundleMonitorWindow : EditorWindow
    {
        private AssetBundleRecordTreeView _recordTreeView;
        private TreeViewState _recordTreeViewState;
        private SearchField _recordSearchField;
        
        private AssetBundleRecordDependencyTreeView _dependencyTreeView;
        private TreeViewState _dependencyTreeViewState;
        private SearchField _dependencySearchField;
        
        private AssetBundleRecord _selectedRecord;
        
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _toolbarButtonStyle;
        
        private bool _showSummary = true;
        private bool _autoRefresh = false;
        private double _lastRefreshTime;
        private const double AutoRefreshInterval = 1.0; // 1 second
        
        [MenuItem("OneAsset/Editor/AssetBundleMonitorWindow")]
        public static void Open()
        {
            var window = GetWindow<AssetBundleMonitorWindow>("AssetBundle Monitor", true);
            window.minSize = new Vector2(1200, 700);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitTreeView();
            RefreshData();
        }
        
        private void Update()
        {
            // Auto refresh
            if (_autoRefresh && AssetBundleMonitor.Instance.IsRecording)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > AutoRefreshInterval)
                {
                    RefreshData();
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }
        }
        
        /// <summary>
        /// Initialize TreeView
        /// </summary>
        private void InitTreeView()
        {
            // Initialize record TreeView
            if (_recordTreeViewState == null)
                _recordTreeViewState = new TreeViewState();
            
            var recordMultiColumnHeaderState = AssetBundleRecordTreeView.CreateDefaultMultiColumnHeaderState();
            var recordMultiColumnHeader = new MultiColumnHeader(recordMultiColumnHeaderState);
            recordMultiColumnHeader.ResizeToFit();
            
            _recordTreeView = new AssetBundleRecordTreeView(_recordTreeViewState, recordMultiColumnHeader);
            _recordTreeView.OnBundleSelected += OnRecordSelected;
            
            _recordSearchField = new SearchField();
            _recordSearchField.downOrUpArrowKeyPressed += _recordTreeView.SetFocusAndEnsureSelectedItem;
            
            // Initialize dependency TreeView
            if (_dependencyTreeViewState == null)
                _dependencyTreeViewState = new TreeViewState();
            
            var dependencyMultiColumnHeaderState = AssetBundleRecordDependencyTreeView.CreateDefaultMultiColumnHeaderState();
            var dependencyMultiColumnHeader = new MultiColumnHeader(dependencyMultiColumnHeaderState);
            dependencyMultiColumnHeader.ResizeToFit();
            
            _dependencyTreeView = new AssetBundleRecordDependencyTreeView(_dependencyTreeViewState, dependencyMultiColumnHeader);
            
            _dependencySearchField = new SearchField();
            _dependencySearchField.downOrUpArrowKeyPressed += _dependencyTreeView.SetFocusAndEnsureSelectedItem;
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
            
            if (_toolbarButtonStyle == null)
            {
                _toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fontStyle = FontStyle.Bold
                };
            }
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            DrawToolbar();
            
            DrawSummarySection();
            
            EditorGUILayout.Space(5);
            
            DrawRecordListSection();
        }
        
        /// <summary>
        /// Draw toolbar
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var isRecording = AssetBundleMonitor.Instance.IsRecording;
                
                // Start/Stop button
                GUI.backgroundColor = isRecording ? Color.red : Color.green;
                if (GUILayout.Button(isRecording ? "⏹ Stop" : "⏺ Start", 
                    _toolbarButtonStyle, GUILayout.Width(80)))
                {
                    if (isRecording)
                    {
                        AssetBundleMonitor.Instance.StopRecording();
                        RefreshData();
                    }
                    else
                    {
                        AssetBundleMonitor.Instance.StartRecording();
                        RefreshData();
                    }
                }
                GUI.backgroundColor = Color.white;
                
                // Clear button
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Clear all recorded data?", "OK", "Cancel"))
                    {
                        AssetBundleMonitor.Instance.ClearSession();
                        RefreshData();
                    }
                }
                
                // Refresh button
                if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RefreshData();
                }
                
                // Auto refresh toggle
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(90));
                
                GUILayout.FlexibleSpace();
                
                // Status display
                var statusText = isRecording ? "● Recording..." : "○ Stopped";
                var statusColor = isRecording ? Color.red : Color.gray;
                var oldColor = GUI.color;
                GUI.color = statusColor;
                GUILayout.Label(statusText, EditorStyles.miniLabel);
                GUI.color = oldColor;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draw summary section
        /// </summary>
        private void DrawSummarySection()
        {
            var session = AssetBundleMonitor.Instance.CurrentSession;
            if (session == null)
            {
                EditorGUILayout.HelpBox("No monitoring data available. Click 'Start' to begin monitoring.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                _showSummary = EditorGUILayout.BeginFoldoutHeaderGroup(_showSummary, "Monitor Summary");
                
                if (_showSummary)
                {
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Session Start:", session.sessionStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    if (!session.isRecording)
                    {
                        DrawLabelField("Session End:", session.sessionEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Duration:", $"{session.GetSessionDuration():F2}s");
                    DrawLabelField("Total Records:", session.records.Count.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    DrawLabelField("Success:", session.GetSuccessCount().ToString(), Color.green);
                    DrawLabelField("Failed:", session.GetFailedCount().ToString(), 
                        session.GetFailedCount() > 0 ? Color.red : Color.gray);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    long totalSize = session.GetTotalLoadedSize();
                    string sizeStr = GetSizeReadable(totalSize);
                    DrawLabelField("Total Loaded Size:", sizeStr);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw record list section
        /// </summary>
        private void DrawRecordListSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Load Record List", _headerStyle);
                GUILayout.FlexibleSpace();
                
                var session = AssetBundleMonitor.Instance.CurrentSession;
                if (session != null)
                {
                    GUILayout.Label($"Total: {session.records.Count}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                {
                    // Left: Record list
                    EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.7f));
                    {
                        // Search box
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        {
                            var searchString = _recordSearchField.OnToolbarGUI(_recordTreeView.searchString);
                            _recordTreeView.searchString = searchString;
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        // TreeView
                        var rect = GUILayoutUtility.GetRect(0, position.height - 200, GUILayout.ExpandWidth(true));
                        _recordTreeView.OnGUI(rect);
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(10);
                    
                    // Right: Detail info
                    EditorGUILayout.BeginVertical();
                    {
                        if (_selectedRecord != null)
                        {
                            DrawRecordDetails(_selectedRecord);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Select a record from the left to view details", MessageType.Info);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw record details
        /// </summary>
        private void DrawRecordDetails(AssetBundleRecord record)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                GUILayout.Label("Record Details", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                DrawLabelField("Bundle Name:", record.bundleName);
                DrawLabelField("Package Name:", record.packageName);
                DrawLabelField("Scene:", record.sceneName);
                
                if (!string.IsNullOrEmpty(record.assetAddress))
                    DrawLabelField("Asset Address:", record.assetAddress);
                
                if (!string.IsNullOrEmpty(record.assetPath))
                    DrawLabelField("Asset Path:", record.assetPath);
                
                DrawLabelField("Load Start:", record.loadStartTime.ToString("HH:mm:ss.fff"));
                DrawLabelField("Load End:", record.loadEndTime.ToString("HH:mm:ss.fff"));
                DrawLabelField("Duration:", record.GetLoadDurationReadable());
                DrawLabelField("Bundle Size:", record.GetBundleSizeReadable());
                DrawLabelField("Ref Count:", record.referenceCount.ToString());
                DrawLabelField("Load Type:", record.loadType);
                
                var statusColor = record.loadSuccess ? Color.green : Color.red;
                DrawLabelField("Load Status:", record.loadSuccess ? "✓ Success" : "✗ Failed", statusColor);
                
                if (!record.loadSuccess && !string.IsNullOrEmpty(record.errorMessage))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Error Message:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(record.errorMessage, MessageType.Error);
                }
                
                EditorGUILayout.Space(5);
                
                // Dependencies list
                if (record.dependencies != null && record.dependencies.Count > 0)
                {
                    EditorGUILayout.LabelField($"Dependency List ({record.dependencies.Count}):", EditorStyles.boldLabel);
                    
                    // Dependency search box
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        var dependencySearchString = _dependencySearchField.OnToolbarGUI(_dependencyTreeView.searchString);
                        _dependencyTreeView.searchString = dependencySearchString;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    var dependencyRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
                    _dependencyTreeView.OnGUI(dependencyRect);
                }
                else
                {
                    EditorGUILayout.LabelField("Dependency List:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("No dependencies", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw label field
        /// </summary>
        private void DrawLabelField(string label, string value, Color? valueColor = null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            
            if (valueColor.HasValue)
            {
                var oldColor = GUI.color;
                GUI.color = valueColor.Value;
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                GUI.color = oldColor;
            }
            else
            {
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Record selection callback
        /// </summary>
        private void OnRecordSelected(AssetBundleRecord record)
        {
            _selectedRecord = record;
            _dependencyTreeView.SetDependencies(record?.dependencies);
            Repaint();
        }
        
        /// <summary>
        /// Refresh data
        /// </summary>
        private void RefreshData()
        {
            var records = AssetBundleMonitor.Instance.GetAllRecords();
            _recordTreeView.SetRecords(records);
            
            // Clear selection if selected record no longer exists
            if (_selectedRecord != null && !records.Contains(_selectedRecord))
            {
                _selectedRecord = null;
                _dependencyTreeView.SetDependencies(null);
            }
            
            Repaint();
        }
        
        /// <summary>
        /// Get readable size format
        /// </summary>
        private string GetSizeReadable(long size)
        {
            if (size < 1024)
                return $"{size}B";
            else if (size < 1024 * 1024)
                return $"{size / 1024.0:F2}KB";
            else if (size < 1024 * 1024 * 1024)
                return $"{size / (1024.0 * 1024.0):F2}MB";
            else
                return $"{size / (1024.0 * 1024.0 * 1024.0):F2}GB";
        }
    }
}
