using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleMonitor
{
    /// <summary>
    /// AssetBundle Load Record TreeView
    /// </summary>
    public class AssetBundleRecordTreeView : TreeView
    {
        private List<AssetBundleRecord> _records;
        
        public delegate void BundleSelectedDelegate(AssetBundleRecord record);
        public event BundleSelectedDelegate OnBundleSelected;
        
        // Column Enum
        enum ColumnType
        {
            BundleName,
            SceneName,
            AssetPath,
            LoadTime,
            LoadDuration,
            ReferenceCount,
            Size,
            Status,
            LoadType
        }
        
        public AssetBundleRecordTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            
            Reload();
        }
        
        /// <summary>
        /// Create default multi-column header state
        /// </summary>
        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bundle Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 200,
                    minWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Scene"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 100,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Path"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 250,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Load Time"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 140,
                    minWidth = 100,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Duration"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 80,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Ref Count"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 70,
                    minWidth = 40,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 80,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Status"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 60,
                    minWidth = 50,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Load Type"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 80,
                    minWidth = 60,
                    autoResize = false
                }
            };
            
            var state = new MultiColumnHeaderState(columns);
            return state;
        }
        
        /// <summary>
        /// Set record data
        /// </summary>
        public void SetRecords(List<AssetBundleRecord> records)
        {
            _records = records;
            Reload();
        }
        
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            if (_records == null || _records.Count == 0)
            {
                root.children = new List<TreeViewItem>();
                return root;
            }
            
            var allItems = new List<TreeViewItem>();
            for (int i = 0; i < _records.Count; i++)
            {
                var item = new TreeViewItem
                {
                    id = i + 1,
                    depth = 0,
                    displayName = _records[i].bundleName
                };
                allItems.Add(item);
            }
            
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            var record = _records[args.item.id - 1];
            
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), record, (ColumnType)args.GetColumn(i), ref args);
            }
        }
        
        private void CellGUI(Rect cellRect, AssetBundleRecord record, ColumnType column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            
            switch (column)
            {
                case ColumnType.BundleName:
                    GUI.Label(cellRect, record.bundleName);
                    break;
                    
                case ColumnType.SceneName:
                    GUI.Label(cellRect, record.sceneName);
                    break;
                    
                case ColumnType.AssetPath:
                    GUI.Label(cellRect, string.IsNullOrEmpty(record.assetPath) ? record.assetAddress : record.assetPath);
                    break;
                    
                case ColumnType.LoadTime:
                    GUI.Label(cellRect, record.loadStartTime.ToString("HH:mm:ss.fff"));
                    break;
                    
                case ColumnType.LoadDuration:
                    GUI.Label(cellRect, record.GetLoadDurationReadable());
                    break;
                    
                case ColumnType.ReferenceCount:
                    GUI.Label(cellRect, record.referenceCount.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                    break;
                    
                case ColumnType.Size:
                    GUI.Label(cellRect, record.GetBundleSizeReadable());
                    break;
                    
                case ColumnType.Status:
                    var statusColor = record.loadSuccess ? Color.green : Color.red;
                    var oldColor = GUI.color;
                    GUI.color = statusColor;
                    GUI.Label(cellRect, record.loadSuccess ? "Success" : "Failed", 
                        new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
                    GUI.color = oldColor;
                    break;
                    
                case ColumnType.LoadType:
                    GUI.Label(cellRect, record.loadType, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                    break;
            }
        }
        
        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            var record = _records[id - 1];
            OnBundleSelected?.Invoke(record);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var record = _records[id - 1];
            
            // On double click, if there is an asset path, try to select the asset in the project
            if (!string.IsNullOrEmpty(record.assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(record.assetPath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
            }
        }
        
        private void OnSortingChanged(MultiColumnHeader header)
        {
            if (_records == null || _records.Count == 0)
                return;
            
            var sortedColumn = header.sortedColumnIndex;
            var ascending = header.IsSortedAscending(sortedColumn);
            
            switch ((ColumnType)sortedColumn)
            {
                case ColumnType.BundleName:
                    _records = ascending 
                        ? _records.OrderBy(r => r.bundleName).ToList()
                        : _records.OrderByDescending(r => r.bundleName).ToList();
                    break;
                    
                case ColumnType.SceneName:
                    _records = ascending 
                        ? _records.OrderBy(r => r.sceneName).ToList()
                        : _records.OrderByDescending(r => r.sceneName).ToList();
                    break;
                    
                case ColumnType.LoadTime:
                    _records = ascending 
                        ? _records.OrderBy(r => r.loadStartTime).ToList()
                        : _records.OrderByDescending(r => r.loadStartTime).ToList();
                    break;
                    
                case ColumnType.LoadDuration:
                    _records = ascending 
                        ? _records.OrderBy(r => r.loadDuration).ToList()
                        : _records.OrderByDescending(r => r.loadDuration).ToList();
                    break;
                    
                case ColumnType.ReferenceCount:
                    _records = ascending 
                        ? _records.OrderBy(r => r.referenceCount).ToList()
                        : _records.OrderByDescending(r => r.referenceCount).ToList();
                    break;
                    
                case ColumnType.Size:
                    _records = ascending 
                        ? _records.OrderBy(r => r.bundleSize).ToList()
                        : _records.OrderByDescending(r => r.bundleSize).ToList();
                    break;
                    
                case ColumnType.Status:
                    _records = ascending 
                        ? _records.OrderBy(r => r.loadSuccess).ToList()
                        : _records.OrderByDescending(r => r.loadSuccess).ToList();
                    break;
                    
                case ColumnType.LoadType:
                    _records = ascending 
                        ? _records.OrderBy(r => r.isAsync).ToList()
                        : _records.OrderByDescending(r => r.isAsync).ToList();
                    break;
            }
            
            Reload();
        }
    }
}

