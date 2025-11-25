using System.Collections.Generic;
using System.Linq;
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

        private double _lastRefreshTime;
        private const double AutoRefreshIntervalSeconds = 0.5;

        // Profiler controls
        private int _currentFrameIndex = 0;
        private int _minFrameIndex = 0;
        private int _maxFrameIndex = 0;
        private bool _autoScrollToLatest = true;

        // Profiler fields
        private bool _showTotal = true;
        private bool _showLoaded = true;
        private bool _showUnloaded = true;

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
            if (AssetBundleMonitor.IsRecording)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > AutoRefreshIntervalSeconds)
                {
                    RefreshData(true);
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

            var dependencyMultiColumnHeaderState =
                AssetBundleRecordDependencyTreeView.CreateDefaultMultiColumnHeaderState();
            var dependencyMultiColumnHeader = new MultiColumnHeader(dependencyMultiColumnHeaderState);
            dependencyMultiColumnHeader.ResizeToFit();

            _dependencyTreeView =
                new AssetBundleRecordDependencyTreeView(_dependencyTreeViewState, dependencyMultiColumnHeader);

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

            EditorGUILayout.Space(5);

            DrawProfilerSection();

            EditorGUILayout.Space(5);

            DrawRecordListSection();
        }

        private void DrawProfilerSection()
        {
            var session = AssetBundleMonitor.CurrentSession;

            EditorGUILayout.BeginVertical(_boxStyle);
            // GUILayout.Label("Profiler", _headerStyle);

            EditorGUILayout.BeginHorizontal();

            // Left: Legend only (always visible)
            EditorGUILayout.BeginVertical(GUILayout.Width(150)); // Increased width for better readability
            {
                GUILayout.Label("AssetBundles", EditorStyles.boldLabel);
                DrawLegendToggle("Total", ref _showTotal, Color.cyan);
                DrawLegendToggle("Loaded", ref _showLoaded, Color.green);
                DrawLegendToggle("Unloaded", ref _showUnloaded, new Color(1f, 0.3f, 0.3f));
            }
            EditorGUILayout.EndVertical();

            // Draw vertical separator line
            var separatorRect =
                GUILayoutUtility.GetRect(1, 150, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            EditorGUI.DrawRect(separatorRect, Color.black);

            // Right: Graph
            if (session?.profilerData != null)
            {
                DrawProfilerGraph(session.profilerData);
            }
            else
            {
                // Empty space when no data
                GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawLegendToggle(string label, ref bool value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            // Draw color box button
            var rect = GUILayoutUtility.GetRect(7, 7, GUILayout.ExpandWidth(false));
            if (Event.current.type == EventType.Repaint)
            {
                var drawRect = new Rect(rect.x, rect.y + 4, rect.width, rect.height); // Center vertically roughly
                EditorGUI.DrawRect(drawRect, value ? color : Color.black);
                // Draw border if black
                if (!value)
                {
                    Handles.color = Color.gray;
                    Handles.DrawWireDisc(drawRect.center, Vector3.forward, 3f);
                }
            }

            GUILayout.Label(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Hit test for the whole row
            var clickRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                value = !value;
                Event.current.Use();
            }
        }

        private void DrawColoredLabel(string text, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, EditorStyles.miniLabel);
            GUI.color = oldColor;
        }

        private void DrawProfilerGraph(List<ProfilerFrameData> data)
        {
            if (data.Count == 0) return;

            // Graph settings
            float height = 150f;

            // Use full width available
            Rect graphRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            float width = graphRect.width;

            float totalBarWidth = width / data.Count;
            float spacing = totalBarWidth > 3f ? 1f : 0f;
            float barWidth = totalBarWidth - spacing;
            if (barWidth < 0.1f) barWidth = 0.1f;

            // Calculate Max Value for scaling
            int maxVal = 100; // Fixed max value as requested
            maxVal = Mathf.Max(maxVal, 1); // Avoid divide by zero

            // Handle Input
            HandleGraphInput(graphRect, data, totalBarWidth);

            // Draw Bars
            if (Event.current.type == EventType.Repaint)
            {
                // Draw Bars First
                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    float x = graphRect.x + i * totalBarWidth;

                    // Draw Total (Cyan)
                    if (_showTotal)
                    {
                        float h = (float) d.totalBundleCount / maxVal * height;
                        if (h > 0)
                            EditorGUI.DrawRect(new Rect(x, graphRect.y + height - h, barWidth, h), Color.cyan);
                    }

                    // Draw Loaded (Green)
                    if (_showLoaded && d.loadedCount > 0)
                    {
                        float h = (float) d.loadedCount / maxVal * height;
                        EditorGUI.DrawRect(new Rect(x, graphRect.y + height - h, barWidth, h), Color.green);
                    }

                    // Draw Unloaded (Red)
                    if (_showUnloaded && d.unloadedCount > 0)
                    {
                        float h = (float) d.unloadedCount / maxVal * height;
                        EditorGUI.DrawRect(new Rect(x, graphRect.y + height - h, barWidth, h),
                            new Color(1f, 0.3f, 0.3f));
                    }
                }

                // Draw horizontal reference lines (75 and 25)
                // Draw 75 line
                float line75Y = graphRect.y + height - (75f / maxVal * height);
                EditorGUI.DrawRect(new Rect(graphRect.x, line75Y, width, 1), new Color(0.5f, 0.5f, 0.5f, 0.8f));

                // Draw 75 label with black background
                float labelWidth = 30f;
                float labelHeight = 16f;
                Rect label75BgRect = new Rect(graphRect.x + 5, line75Y - labelHeight / 2, labelWidth,
                    labelHeight);
                EditorGUI.DrawRect(label75BgRect, Color.black);
                GUI.Label(label75BgRect, "75", EditorStyles.miniLabel);

                // Draw 25 line
                float line25Y = graphRect.y + height - (25f / maxVal * height);
                EditorGUI.DrawRect(new Rect(graphRect.x, line25Y, width, 1), new Color(0.5f, 0.5f, 0.5f, 0.8f));

                // Draw 25 label with black background
                Rect label25BgRect = new Rect(graphRect.x + 5, line25Y - labelHeight / 2, labelWidth,
                    labelHeight);
                EditorGUI.DrawRect(label25BgRect, Color.black);
                GUI.Label(label25BgRect, "25", EditorStyles.miniLabel);

                // Draw Selection Overlay (Top Layer)
                for (int i = 0; i < data.Count; i++)
                {
                    var d = data[i];
                    if (d.frameIndex == _currentFrameIndex)
                    {
                        float x = graphRect.x + i * totalBarWidth;
                        // White selection bar with adaptive width
                        EditorGUI.DrawRect(new Rect(x, graphRect.y, totalBarWidth, height),
                            new Color(1f, 1f, 1f, 0.6f));

                        // Draw Bubble Info
                        DrawSelectionBubble(d, new Rect(x, graphRect.y, totalBarWidth, height), graphRect);
                    }
                }
            }
        }

        private void HandleGraphInput(Rect graphRect, List<ProfilerFrameData> data, float totalBarWidth)
        {
            Event evt = Event.current;
            int newFrameIndex = -1;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (graphRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = controlID;

                        float localX = evt.mousePosition.x - graphRect.x;
                        int index = Mathf.FloorToInt(localX / totalBarWidth);
                        if (index < 0) index = 0;
                        if (index >= data.Count) index = data.Count - 1;
                        if (index >= 0 && index < data.Count) newFrameIndex = data[index].frameIndex;

                        evt.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        float localX = evt.mousePosition.x - graphRect.x;
                        int index = Mathf.FloorToInt(localX / totalBarWidth);
                        if (index < 0) index = 0;
                        if (index >= data.Count) index = data.Count - 1;
                        if (index >= 0 && index < data.Count) newFrameIndex = data[index].frameIndex;

                        evt.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
            }

            // Keyboard
            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.LeftArrow)
                {
                    var currentIdx = data.FindIndex(d => d.frameIndex == _currentFrameIndex);
                    if (currentIdx > 0)
                        newFrameIndex = data[currentIdx - 1].frameIndex;
                    else if (currentIdx == -1 && data.Count > 0)
                        newFrameIndex = data[data.Count - 1].frameIndex;

                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.RightArrow)
                {
                    var currentIdx = data.FindIndex(d => d.frameIndex == _currentFrameIndex);
                    if (currentIdx >= 0 && currentIdx < data.Count - 1)
                        newFrameIndex = data[currentIdx + 1].frameIndex;
                    else if (currentIdx == -1 && data.Count > 0)
                        newFrameIndex = data[0].frameIndex;

                    evt.Use();
                }
            }

            if (newFrameIndex != -1 && newFrameIndex != _currentFrameIndex)
            {
                _currentFrameIndex = newFrameIndex;
                _autoScrollToLatest = false;
                RefreshData(false);
                Repaint();
            }
        }

        private void DrawSelectionBubble(ProfilerFrameData d, Rect selectionRect, Rect graphRect)
        {
            // Prepare content
            var contents = new List<(string label, string value, Color color)>();
            contents.Add(("Frame", d.frameIndex.ToString(), Color.white));
            if (_showTotal) contents.Add(("Total", d.totalBundleCount.ToString(), Color.cyan));
            if (_showLoaded) contents.Add(("Loaded", d.loadedCount.ToString(), Color.green));
            if (_showUnloaded) contents.Add(("Unloaded", d.unloadedCount.ToString(), new Color(1f, 0.3f, 0.3f)));

            if (contents.Count == 0) return;

            float lineHeight = 16f;
            float width = 140f;
            float padding = 5f;
            float height = contents.Count * lineHeight + padding * 2;

            // Position
            float x = selectionRect.x + selectionRect.width + 5; // Right side
            float y = selectionRect.y;

            // Check bounds (flip to left if not enough space on right)
            if (x + width > graphRect.xMax)
            {
                x = selectionRect.x - width - 5;
            }

            // Clamp Y
            if (y + height > graphRect.yMax)
            {
                y = graphRect.yMax - height;
            }

            Rect bubbleRect = new Rect(x, y, width, height);

            // Draw Bubble Background (Black)
            EditorGUI.DrawRect(bubbleRect, Color.black);

            // Draw Content
            float currentY = y + padding;
            foreach (var item in contents)
            {
                var rect = new Rect(x + padding, currentY, width - padding * 2, lineHeight);
                var oldColor = GUI.color;
                GUI.color = item.color;
                GUI.Label(rect, $"{item.label}: {item.value}", EditorStyles.boldLabel);
                GUI.color = oldColor;
                currentY += lineHeight;
            }
        }

        /// <summary>
        /// Draw toolbar
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var isRecording = AssetBundleMonitor.IsRecording;

                // Record Button
                var oldColor = GUI.color;
                if (isRecording) GUI.color = Color.red;
                var recordContent = EditorGUIUtility.IconContent("Animation.Record");
                recordContent.tooltip = isRecording ? "Stop Recording" : "Start Recording";

                if (GUILayout.Button(recordContent, EditorStyles.toolbarButton, GUILayout.Width(30)))
                {
                    if (isRecording)
                    {
                        AssetBundleMonitor.StopRecording();
                        _autoScrollToLatest = false;
                    }
                    else
                    {
                        AssetBundleMonitor.StartRecording();
                        _autoScrollToLatest = true;
                    }

                    RefreshData(true);
                }

                GUI.color = oldColor;

                // Clear
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Clear all recorded data?", "OK", "Cancel"))
                    {
                        AssetBundleMonitor.ClearSession();
                        _currentFrameIndex = 0;
                        _minFrameIndex = 0;
                        _maxFrameIndex = 0;
                        _selectedRecord = null;
                        RefreshData(true);
                    }
                }

                // Load
                if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    string path = EditorUtility.OpenFilePanel("Load Session", "", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetBundleMonitor.LoadSession(path);
                        _autoScrollToLatest = false;
                        RefreshData(true);

                        // Jump to start after load - use profilerData if available
                        var loadedSession = AssetBundleMonitor.CurrentSession;
                        if (loadedSession?.profilerData != null && loadedSession.profilerData.Count > 0)
                        {
                            _currentFrameIndex = loadedSession.profilerData[0].frameIndex;
                        }
                        else
                        {
                            _currentFrameIndex = _minFrameIndex;
                        }

                        RefreshData(false);
                    }
                }

                // Save
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    string path = EditorUtility.SaveFilePanel("Save Session", "",
                        "ABMonitorSession_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"), "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetBundleMonitor.SaveSession(path);
                    }
                }

                GUILayout.Space(20);

                // Frame Navigation
                var session = AssetBundleMonitor.CurrentSession;
                var profilerData = session?.profilerData;

                if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.FirstKey"), EditorStyles.toolbarButton,
                    GUILayout.Width(30)))
                {
                    _autoScrollToLatest = false;
                    if (profilerData != null && profilerData.Count > 0)
                    {
                        _currentFrameIndex = profilerData[0].frameIndex;
                    }
                    else
                    {
                        _currentFrameIndex = _minFrameIndex;
                    }

                    RefreshData(false);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.PrevKey"), EditorStyles.toolbarButton,
                    GUILayout.Width(30)))
                {
                    _autoScrollToLatest = false;
                    if (profilerData != null && profilerData.Count > 0)
                    {
                        var currentIdx = profilerData.FindIndex(d => d.frameIndex == _currentFrameIndex);
                        if (currentIdx > 0)
                        {
                            _currentFrameIndex = profilerData[currentIdx - 1].frameIndex;
                        }
                        else if (currentIdx == -1 && profilerData.Count > 0)
                        {
                            _currentFrameIndex = profilerData[profilerData.Count - 1].frameIndex;
                        }
                    }
                    else
                    {
                        _currentFrameIndex = Mathf.Max(_minFrameIndex, _currentFrameIndex - 1);
                    }

                    RefreshData(false);
                }

                int lastFrameIndex = 0;
                if (profilerData != null && profilerData.Count > 0)
                {
                    lastFrameIndex = profilerData[profilerData.Count - 1].frameIndex;
                }
                else
                {
                    lastFrameIndex = _maxFrameIndex;
                }

                GUILayout.Label($"Frame: {_currentFrameIndex} / {lastFrameIndex}", EditorStyles.miniLabel,
                    GUILayout.Width(120));

                if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.NextKey"), EditorStyles.toolbarButton,
                    GUILayout.Width(30)))
                {
                    _autoScrollToLatest = false;
                    if (profilerData != null && profilerData.Count > 0)
                    {
                        var currentIdx = profilerData.FindIndex(d => d.frameIndex == _currentFrameIndex);
                        if (currentIdx >= 0 && currentIdx < profilerData.Count - 1)
                        {
                            _currentFrameIndex = profilerData[currentIdx + 1].frameIndex;
                        }
                        else if (currentIdx == -1 && profilerData.Count > 0)
                        {
                            _currentFrameIndex = profilerData[0].frameIndex;
                        }
                    }
                    else
                    {
                        _currentFrameIndex = Mathf.Min(_maxFrameIndex, _currentFrameIndex + 1);
                    }

                    RefreshData(false);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Animation.LastKey"), EditorStyles.toolbarButton,
                    GUILayout.Width(30)))
                {
                    _autoScrollToLatest = false;
                    if (profilerData != null && profilerData.Count > 0)
                    {
                        _currentFrameIndex = profilerData[profilerData.Count - 1].frameIndex;
                    }
                    else
                    {
                        _currentFrameIndex = _maxFrameIndex;
                    }

                    RefreshData(false);
                }

                GUILayout.FlexibleSpace();

                // Status display
                if (_autoScrollToLatest)
                {
                    GUILayout.Label("Auto-Scroll", EditorStyles.miniLabel);
                }

                if (GUILayout.Button(_autoScrollToLatest ? "Current" : "Current (Off)", EditorStyles.toolbarButton,
                    GUILayout.Width(90)))
                {
                    _autoScrollToLatest = !_autoScrollToLatest;
                    if (_autoScrollToLatest)
                    {
                        RefreshData(true);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw record list section
        /// </summary>
        private void DrawRecordListSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Frame {_currentFrameIndex} Records", _headerStyle);
                GUILayout.FlexibleSpace();

                // Count for current frame
                var visibleCount = _recordTreeView.GetRows()?.Count ?? 0;
                GUILayout.Label($"Count: {visibleCount}", EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Record list (Top)
                EditorGUILayout.BeginVertical();
                {
                    // Search box
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        var searchString = _recordSearchField.OnToolbarGUI(_recordTreeView.searchString);
                        _recordTreeView.searchString = searchString;
                    }
                    EditorGUILayout.EndHorizontal();

                    // TreeView
                    float recordListHeight = Mathf.Max(200, (position.height - 300) * 0.5f);
                    var rect = GUILayoutUtility.GetRect(0, recordListHeight, GUILayout.ExpandWidth(true));
                    _recordTreeView.OnGUI(rect);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // Record details (Bottom)
                EditorGUILayout.BeginVertical();
                {
                    DrawRecordDetails(_selectedRecord);
                }
                EditorGUILayout.EndVertical();
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

                if (record == null)
                {
                    EditorGUILayout.HelpBox("Select a record from the list above to view details", MessageType.Info);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.Space(5);

                DrawLabelField("Bundle Name:", record.bundleName);
                DrawLabelField("Frame Index:", record.frameIndex.ToString());
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
                    EditorGUILayout.LabelField($"Dependency List ({record.dependencies.Count}):",
                        EditorStyles.boldLabel);

                    // Dependency search box
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    {
                        var dependencySearchString =
                            _dependencySearchField.OnToolbarGUI(_dependencyTreeView.searchString);
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
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                GUI.color = oldColor;
            }
            else
            {
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
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
        private void RefreshData(bool fetchAll = true)
        {
            var records = AssetBundleMonitor.GetAllRecords();

            if (records != null && records.Count > 0)
            {
                if (fetchAll)
                {
                    // In a real implementation we might want to cache min/max, 
                    // but iterating thousands of records is fast enough for Editor.
                    _minFrameIndex = records.Min(r => r.frameIndex);
                    _maxFrameIndex = records.Max(r => r.frameIndex);

                    if (_autoScrollToLatest)
                    {
                        _currentFrameIndex = _maxFrameIndex;
                    }
                }

                // Filter for current frame
                var frameRecords = records.Where(r => r.frameIndex == _currentFrameIndex).ToList();
                _recordTreeView.SetRecords(frameRecords);
            }
            else
            {
                _minFrameIndex = 0;
                _maxFrameIndex = 0;
                _currentFrameIndex = 0;
                _recordTreeView.SetRecords(new List<AssetBundleRecord>());
            }

            // Clear selection if it's no longer visible (optional, but good for avoiding confusion)
            if (_selectedRecord != null)
            {
                // We don't necessarily need to clear selection if we switch frames, 
                // but the TreeView will rebuild and might lose the selection state anyway if the item isn't in the list.
                // AssetBundleRecordTreeView usually handles SetSelection.
                // Check if selected record is in current frame list
                var rows = _recordTreeView.GetRows();
                bool found = false;
                if (rows != null)
                {
                    // This is expensive if list is huge, but fine for typical usage.
                    // Actually TreeView handles selection logic internally usually.
                }
            }

            Repaint();
        }
    }
}