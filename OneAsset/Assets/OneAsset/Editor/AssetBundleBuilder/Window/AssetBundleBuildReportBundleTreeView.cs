using System;
using System.Collections.Generic;
using OneAsset.Editor.AssetBundleBuilder.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    /// <summary>
    /// Bundle TreeView item
    /// </summary>
    public class AssetBundleBuildReportBundleTreeViewItem : TreeViewItem
    {
        public BundleReportInfo BundleInfo { get; set; }
    }

    /// <summary>
    /// Bundle TreeView with multi-column header
    /// </summary>
    public class AssetBundleBuildReportBundleTreeView : TreeView
    {
        // Column indices
        private enum ColumnId
        {
            BundleName = 0,
            BundleSize = 1,
            AssetCount = 2
        }
        
        private List<BundleReportInfo> _bundles;
        
        public System.Action<BundleReportInfo> OnBundleSelected;

        public AssetBundleBuildReportBundleTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) 
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
                    headerContent = new GUIContent("Bundle Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 300,
                    minWidth = 150,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 100,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Count"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 100,
                    minWidth = 80,
                    autoResize = true,
                    allowToggleVisibility = true,
                    canSort = true
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        /// <summary>
        /// Sets the bundle data to display
        /// </summary>
        public void SetBundles(List<BundleReportInfo> bundles)
        {
            _bundles = bundles;
            Reload();
        }

        /// <summary>
        /// Handles sorting when column header is clicked
        /// </summary>
        private void OnSortingChanged(MultiColumnHeader header)
        {
            if (_bundles == null || _bundles.Count <= 1)
                return;

            var sortedColumn = header.sortedColumnIndex;
            var ascending = header.IsSortedAscending(sortedColumn);

            switch ((ColumnId)sortedColumn)
            {
                case ColumnId.BundleName:
                    _bundles.Sort((a, b) => ascending 
                        ? string.Compare(a.bundleName, b.bundleName, StringComparison.Ordinal) 
                        : string.Compare(b.bundleName, a.bundleName, StringComparison.Ordinal));
                    break;
                case ColumnId.BundleSize:
                    _bundles.Sort((a, b) => ascending 
                        ? a.bundleSize.CompareTo(b.bundleSize) 
                        : b.bundleSize.CompareTo(a.bundleSize));
                    break;
                case ColumnId.AssetCount:
                    _bundles.Sort((a, b) => ascending 
                        ? a.assetCount.CompareTo(b.assetCount) 
                        : b.assetCount.CompareTo(a.assetCount));
                    break;
            }

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            if (_bundles == null || _bundles.Count == 0)
            {
                root.children = new List<TreeViewItem>
                {
                    new TreeViewItem { id = 1, depth = 0, displayName = "No bundles" }
                };
                return root;
            }

            var allItems = new List<TreeViewItem>();
            int id = 1;

            foreach (var bundle in _bundles)
            {
                var item = new AssetBundleBuildReportBundleTreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = bundle.bundleName,
                    BundleInfo = bundle
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
            var item = args.item as AssetBundleBuildReportBundleTreeViewItem;
            if (item?.BundleInfo == null)
            {
                base.RowGUI(args);
                return;
            }

            var bundle = item.BundleInfo;

            // Draw each column
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (ColumnId)args.GetColumn(i), ref args);
            }
        }

        /// <summary>
        /// Draws the content of a single cell
        /// </summary>
        private void CellGUI(Rect cellRect, AssetBundleBuildReportBundleTreeViewItem item, ColumnId column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case ColumnId.BundleName:
                    // Draw bundle name with default label style
                    EditorGUI.LabelField(cellRect, item.BundleInfo.bundleName);
                    break;

                case ColumnId.BundleSize:
                    // Draw bundle size in readable format
                    EditorGUI.LabelField(cellRect, item.BundleInfo.bundleSizeReadable);
                    break;

                case ColumnId.AssetCount:
                    // Draw asset count
                    EditorGUI.LabelField(cellRect, item.BundleInfo.assetCount.ToString());
                    break;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count > 0)
            {
                var item = FindItem(selectedIds[0], rootItem) as AssetBundleBuildReportBundleTreeViewItem;
                OnBundleSelected?.Invoke(item?.BundleInfo);
            }
        }
    }
}