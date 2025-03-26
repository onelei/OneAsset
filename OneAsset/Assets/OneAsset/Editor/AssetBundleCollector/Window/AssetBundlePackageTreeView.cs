using System;
using System.Collections.Generic;
using OneAsset.Editor.AssetBundleCollector.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    internal sealed class AssetBundlePackageTreeViewItem : TreeViewItem
    {
        public readonly AssetBundleCollectorPackage data;

        public AssetBundlePackageTreeViewItem(int index, AssetBundleCollectorPackage package)
        {
            data = package;
            id = index;
            displayName = package.packageName;
            depth = 0;
        }
    }

    public class AssetBundlePackageTreeView : TreeView
    {
        public Action<AssetBundleCollectorPackage> OnSelectedChange;
        private readonly SearchField _searchField;
        private const float Offset = 2;
        private readonly List<TreeViewItem> _items = new List<TreeViewItem>();
        private readonly Dictionary<int, TreeViewItem> _itemsMap = new Dictionary<int, TreeViewItem>();
        private List<AssetBundleCollectorPackage> _packages;

        public AssetBundleCollectorPackage SelectedPackage { get; private set; }

        public AssetBundlePackageTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(List<AssetBundleCollectorPackage> datas)
        {
            _packages = datas;
            _items.Clear();
            _itemsMap.Clear();
            for (var i = 0; i < datas.Count; i++)
            {
                var package = datas[i];
                var id = i + 1;
                var item = new AssetBundlePackageTreeViewItem(id, package);
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
            rect.height = height - (rect.height + Offset) * 2;
            base.OnGUI(rect);
            //Button
            rect.y += rect.height + Offset;
            rect.width = width * 0.5f;
            rect.height = singleLineHeight;
            if (GUI.Button(rect, "-"))
            {
                if (SelectedPackage == null)
                    return;
                if (EditorUtility.DisplayDialog("Remove Confirm", "Are you sure?", "OK", "Cancel"))
                {
                    _packages.Remove(SelectedPackage);
                    SetData(_packages);
                }
            }

            rect.x += rect.width;
            if (GUI.Button(rect, "+"))
            {
                _packages.Add(new AssetBundleCollectorPackage {packageName = "CustomPackage"});
                SetData(_packages);
            }
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
                if (!(item is AssetBundlePackageTreeViewItem treeViewItem)) return;
                SelectedPackage = treeViewItem.data;
                OnSelectedChange?.Invoke(SelectedPackage);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AssetBundlePackageTreeViewItem) args.item;
            DefaultGUI.Label(args.rowRect, $"{item.data.packageName}({item.data.packageDesc})", args.selected,
                args.focused);
        }
    }
}