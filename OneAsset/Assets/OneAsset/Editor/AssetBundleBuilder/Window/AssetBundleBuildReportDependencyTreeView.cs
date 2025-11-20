using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    /// <summary>
    /// Dependency TreeView item
    /// </summary>
    public class AssetBundleBuildReportDependencyTreeViewItem : TreeViewItem
    {
        public string DependencyName { get; set; }
    }

    /// <summary>
    /// Dependency TreeView with search functionality
    /// </summary>
    public class AssetBundleBuildReportDependencyTreeView : TreeView
    {
        // Column indices
        private enum ColumnId
        {
            DependencyName = 0
        }
        
        private List<string> _dependencies;

        public AssetBundleBuildReportDependencyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) 
            : base(state, multiColumnHeader)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = 20;
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
                    headerContent = new GUIContent("Dependency Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 400,
                    minWidth = 200,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        /// <summary>
        /// Sets the dependency data to display
        /// </summary>
        public void SetDependencies(List<string> dependencies)
        {
            _dependencies = dependencies;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            
            if (_dependencies == null || _dependencies.Count == 0)
            {
                root.children = new List<TreeViewItem>
                {
                    new TreeViewItem { id = 1, depth = 0, displayName = "No dependencies" }
                };
                return root;
            }

            var allItems = new List<TreeViewItem>();
            int id = 1;

            foreach (var dependency in _dependencies)
            {
                var item = new AssetBundleBuildReportDependencyTreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = dependency,
                    DependencyName = dependency
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
            var item = args.item as AssetBundleBuildReportDependencyTreeViewItem;
            if (item?.DependencyName == null)
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
        private void CellGUI(Rect cellRect, AssetBundleBuildReportDependencyTreeViewItem item, ColumnId column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case ColumnId.DependencyName:
                    // Draw dependency name
                    EditorGUI.LabelField(cellRect, item.DependencyName);
                    break;
            }
        }

        /// <summary>
        /// Filters dependencies based on search string
        /// </summary>
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            var dependencyItem = item as AssetBundleBuildReportDependencyTreeViewItem;
            if (dependencyItem?.DependencyName == null)
                return base.DoesItemMatchSearch(item, search);

            var searchLower = search.ToLower();
            return dependencyItem.DependencyName.ToLower().Contains(searchLower);
        }
    }
}

