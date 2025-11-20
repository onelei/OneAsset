using System.Collections.Generic;
using System.Linq;
using OneAsset.Editor.AssetBundleBuilder.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    /// <summary>
    /// Asset TreeView item
    /// </summary>
    public class AssetBundleBuildReportAssetTreeViewItem : TreeViewItem
    {
        public AssetReportInfo AssetInfo { get; set; }
    }

    /// <summary>
    /// Asset TreeView with multi-column header and search functionality
    /// </summary>
    public class AssetBundleBuildReportAssetTreeView : TreeView
    {
        // Column indices
        private enum ColumnId
        {
            Address = 0,
            AssetPath = 1
        }
        
        private List<AssetReportInfo> _assets;
        private List<AssetReportInfo> _filteredAssets;

        public AssetBundleBuildReportAssetTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) 
            : base(state, multiColumnHeader)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        /// <summary>
        /// Creates default multi-column header state
        /// </summary>
        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Address"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 200,
                    minWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Path"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 300,
                    minWidth = 150,
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = true
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        /// <summary>
        /// Sets the asset data to display
        /// </summary>
        public void SetAssets(List<AssetReportInfo> assets)
        {
            _assets = assets;
            _filteredAssets = assets;
            Reload();
        }

        /// <summary>
        /// Handles sorting when column header is clicked
        /// </summary>
        private void OnSortingChanged(MultiColumnHeader header)
        {
            if (_filteredAssets == null || _filteredAssets.Count <= 1)
                return;

            var sortedColumn = header.sortedColumnIndex;
            var ascending = header.IsSortedAscending(sortedColumn);

            switch ((ColumnId)sortedColumn)
            {
                case ColumnId.Address:
                    _filteredAssets.Sort((a, b) => ascending 
                        ? a.address.CompareTo(b.address) 
                        : b.address.CompareTo(a.address));
                    break;
                case ColumnId.AssetPath:
                    _filteredAssets.Sort((a, b) => ascending 
                        ? a.assetPath.CompareTo(b.assetPath) 
                        : b.assetPath.CompareTo(a.assetPath));
                    break;
            }

            Reload();
        }

        /// <summary>
        /// Filters assets based on search string
        /// </summary>
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var assetItem = item as AssetBundleBuildReportAssetTreeViewItem;
            if (assetItem?.AssetInfo == null)
                return base.DoesItemMatchSearch(item, search);

            var searchLower = search.ToLower();
            return assetItem.AssetInfo.address.ToLower().Contains(searchLower) ||
                   assetItem.AssetInfo.assetPath.ToLower().Contains(searchLower);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            if (_filteredAssets == null || _filteredAssets.Count == 0)
            {
                root.children = new List<TreeViewItem>
                {
                    new TreeViewItem { id = 1, depth = 0, displayName = "No assets" }
                };
                return root;
            }

            var allItems = new List<TreeViewItem>();
            int id = 1;

            foreach (var asset in _filteredAssets)
            {
                var item = new AssetBundleBuildReportAssetTreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = asset.address,
                    AssetInfo = asset
                };
                allItems.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        /// <summary>
        /// Draws the row GUI for each item
        /// </summary>
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as AssetBundleBuildReportAssetTreeViewItem;
            if (item?.AssetInfo == null)
            {
                base.RowGUI(args);
                return;
            }

            // Draw each column
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (ColumnId)args.GetColumn(i), ref args);
            }
        }

        /// <summary>
        /// Draws the content of a single cell
        /// </summary>
        private void CellGUI(Rect cellRect, AssetBundleBuildReportAssetTreeViewItem item, ColumnId column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case ColumnId.Address:
                    // Draw asset address with default label style
                    EditorGUI.LabelField(cellRect, item.AssetInfo.address);
                    break;

                case ColumnId.AssetPath:
                    // Draw asset path
                    EditorGUI.LabelField(cellRect, item.AssetInfo.assetPath);
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as AssetBundleBuildReportAssetTreeViewItem;
            if (item?.AssetInfo != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetInfo.assetPath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }
    }

}