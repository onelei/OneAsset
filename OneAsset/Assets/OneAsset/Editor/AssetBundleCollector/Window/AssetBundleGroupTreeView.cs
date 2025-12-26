using System;
using System.Collections.Generic;
using OneAsset.Editor.AssetBundleCollector.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    internal sealed class AssetBundleGroupTreeViewItem : TreeViewItem
    {
        public AssetBundleCollectorGroup data;

        public AssetBundleGroupTreeViewItem(int index, AssetBundleCollectorGroup group)
        {
            data = group;
            id = index;
            displayName = group.groupName;
            depth = 0;
        }
    }

    public class AssetBundleGroupTreeView : TreeView
    {
        public Action<AssetBundleCollectorGroup> OnSelectedChange;
        private readonly SearchField _searchField;
        private const float Offset = 2;
        private readonly List<TreeViewItem> _items = new List<TreeViewItem>();
        private readonly Dictionary<int, TreeViewItem> _itemsMap = new Dictionary<int, TreeViewItem>();
        private AssetBundleCollectorPackage _package;

        public AssetBundleCollectorGroup SelectedGroup { get; private set; }

        public AssetBundleGroupTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(AssetBundleCollectorPackage data)
        {
            _package = data;
            _items.Clear();
            _itemsMap.Clear();
            for (var i = 0; i < _package.groups.Count; i++)
            {
                var group = _package.groups[i];
                var id = i + 1;
                var item = new AssetBundleGroupTreeViewItem(id, group);
                _itemsMap.Add(id, item);
                _items.Add(item);
            }

            Reload();
            
            // The first item is automatically selected and the change is triggered, creating a chain reaction.
            if (_items.Count > 0)
            {
                SetSelection(new List<int> { _items[0].id });
                SelectedGroup = ((AssetBundleGroupTreeViewItem)_items[0]).data;
                OnSelectedChange?.Invoke(SelectedGroup);
            }
            else
            {
                SelectedGroup = null;
                OnSelectedChange?.Invoke(null);
            }
        }

        public override void OnGUI(Rect rect)
        {
            if (_package == null)
                return;
            var height = rect.height;
            var width = rect.width;
            var singleLineHeight = EditorGUIUtility.singleLineHeight;
            //packageName
            rect.height = singleLineHeight;
            EditorGUI.LabelField(rect, "Package Name");
            rect.y += rect.height + Offset;
            _package.packageName = EditorGUI.TextField(rect, _package.packageName);
            //packageDesc
            rect.y += rect.height + Offset;
            EditorGUI.LabelField(rect, "Package Desc");
            rect.y += rect.height + Offset;
            _package.packageDesc = EditorGUI.TextField(rect, _package.packageDesc);
            //SearchField
            rect.y += rect.height + Offset;
            searchString = _searchField.OnGUI(rect, searchString);
            //TreeView
            rect.y += rect.height + Offset;
            rect.height = height - (singleLineHeight + Offset) * 6;
            base.OnGUI(rect);
            //Button
            rect.y += rect.height + Offset;
            rect.width = width * 0.5f;
            rect.height = singleLineHeight;
            if (GUI.Button(rect, "-"))
            {
                if (SelectedGroup == null)
                    return;
                if (EditorUtility.DisplayDialog("Remove Confirm", "Are you sure?", "OK", "Cancel"))
                {
                    _package.groups.Remove(SelectedGroup);
                    SetData(_package);
                }
            }

            rect.x += rect.width;
            if (GUI.Button(rect, "+"))
            {
                _package.groups.Add(new AssetBundleCollectorGroup() {groupName = "CustomGroup"});
                SetData(_package);
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
                if (!(item is AssetBundleGroupTreeViewItem treeViewItem)) return;
                SelectedGroup = treeViewItem.data;
                OnSelectedChange?.Invoke(SelectedGroup);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AssetBundleGroupTreeViewItem) args.item;
            DefaultGUI.Label(args.rowRect, $"{item.data.groupName}({item.data.groupDesc})", args.selected,
                args.focused);
        }
    }
}