using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using OneAsset.Runtime.Loader;

namespace OneAsset.Editor.AssetBundleMonitor
{
    /// <summary>
    /// AssetBundle Dependency TreeView
    /// </summary>
    public class AssetBundleRecordDependencyTreeView : TreeView
    {
        private List<string> _dependencies;
        
        // Column Enum
        enum ColumnType
        {
            Index,
            DependencyName
        }
        
        public AssetBundleRecordDependencyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            
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
                    headerContent = new GUIContent("#"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 40,
                    minWidth = 40,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Dependency Bundle Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 400,
                    minWidth = 200,
                    autoResize = true,
                    allowToggleVisibility = false
                }
            };
            
            var state = new MultiColumnHeaderState(columns);
            return state;
        }
        
        /// <summary>
        /// Set dependency data
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
                root.children = new List<TreeViewItem>();
                return root;
            }
            
            var allItems = new List<TreeViewItem>();
            for (int i = 0; i < _dependencies.Count; i++)
            {
                var item = new TreeViewItem
                {
                    id = i + 1,
                    depth = 0,
                    displayName = _dependencies[i]
                };
                allItems.Add(item);
            }
            
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            var dependency = _dependencies[args.item.id - 1];
            
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), dependency, (ColumnType)args.GetColumn(i), args.item.id);
            }
        }
        
        private void CellGUI(Rect cellRect, string dependency, ColumnType column, int itemId)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            
            switch (column)
            {
                case ColumnType.Index:
                    GUI.Label(cellRect, itemId.ToString(), new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
                    break;
                    
                case ColumnType.DependencyName:
                    GUI.Label(cellRect, dependency);
                    break;
            }
        }
    }
}

