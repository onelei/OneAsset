using System;
using System.Collections.Generic;
using OneAsset.Editor.AssetBundleBuilder.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleBuilder.Window
{
    internal sealed class AssetBundleBuilderPackageTreeViewItem : TreeViewItem
    {
        public readonly AssetBundleBuilderPackage data;

        public AssetBundleBuilderPackageTreeViewItem(int index, AssetBundleBuilderPackage package)
        {
            data = package;
            id = index;
            displayName = package.packageName;
            depth = 0;
        }
    }

    public class AssetBundleBuilderPackageTreeView : TreeView
    {
        public Action<AssetBundleBuilderPackage> OnSelectedChange;
        private readonly SearchField _searchField;
        private const float Offset = 2;
        private readonly List<TreeViewItem> _items = new List<TreeViewItem>();
        private readonly Dictionary<int, TreeViewItem> _itemsMap = new Dictionary<int, TreeViewItem>();
        private List<AssetBundleBuilderPackage> _packages;

        public AssetBundleBuilderPackage SelectedPackage { get; private set; }

        public AssetBundleBuilderPackageTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(List<AssetBundleBuilderPackage> datas)
        {
            _packages = datas;
            _items.Clear();
            _itemsMap.Clear();
            for (var i = 0; i < datas.Count; i++)
            {
                var package = datas[i];
                var id = i + 1;
                var item = new AssetBundleBuilderPackageTreeViewItem(id, package);
                _itemsMap.Add(id, item);
                _items.Add(item);
            }

            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            var height = rect.height;
            var width = rect.width;
            var singleLineHeight = EditorGUIUtility.singleLineHeight;
            //SearchField
            rect.height = singleLineHeight;
            searchString = _searchField.OnGUI(rect, searchString);
            //TreeView
            rect.y += rect.height + Offset;
            rect.height = height - (rect.height + Offset) * 1;
            base.OnGUI(rect); 
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            SetupParentsAndChildrenFromDepths(root, _items);
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds != null && selectedIds.Count > 0)
            {
                var selectedId = selectedIds[0];
                if (!_itemsMap.TryGetValue(selectedId, out var item)) return;
                if (!(item is AssetBundleBuilderPackageTreeViewItem treeViewItem)) return;
                SelectedPackage = treeViewItem.data;
                OnSelectedChange?.Invoke(SelectedPackage);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AssetBundleBuilderPackageTreeViewItem) args.item;
            DefaultGUI.Label(args.rowRect, $"{item.data.packageName})", args.selected,
                args.focused);
        }
    }
}