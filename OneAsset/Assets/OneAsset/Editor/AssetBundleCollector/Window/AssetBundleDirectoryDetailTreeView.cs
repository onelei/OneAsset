using System;
using System.Collections.Generic;
using OneAsset.Editor.AssetBundleCollector.Data;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    internal sealed class AssetBundleDirectoryDetailTreeViewItem : TreeViewItem
    {
        public AssetBundleDirectoryDetailTreeViewItem(int index, string assetPath)
        {
            id = index;
            displayName = assetPath;
            depth = 0;
        }
    }

    public class AssetBundleDirectoryDetailTreeView : TreeView
    {
        private readonly SearchField _searchField;
        private const float Offset = 2;
        private readonly List<TreeViewItem> _items = new List<TreeViewItem>();
        private readonly Dictionary<int, TreeViewItem> _itemsMap = new Dictionary<int, TreeViewItem>();
        private AssetBundleDirectory _directory;
        
        public AssetBundleDirectoryDetailTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(AssetBundleDirectory data)
        {
            if (data == null)
                return;
            _directory = data;
            _items.Clear();
            _itemsMap.Clear();
            var assets = data.GetMainAssets();
            for (var i = 0; i < assets.Count; i++)
            {
                var package = assets[i];
                var id = i + 1;
                var item = new AssetBundleDirectoryDetailTreeViewItem(id, package);
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
            rect.height = height - (rect.height + Offset);
            base.OnGUI(rect);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            SetupParentsAndChildrenFromDepths(root, _items);
            return root;
        }
    }
}