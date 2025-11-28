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
        private AssetBundleDirectory _directory;

        public AssetBundleDirectoryDetailTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(string groupName, AssetBundleDirectory data)
        {
            if (data == null)
                return;
            _directory = data;
            _items.Clear();
            var mainAssets = data.GetMainAssets();
            var rule = data.GetAddressRule();

            for (var i = 0; i < mainAssets.Count; i++)
            {
                var assetPath = mainAssets[i];
                var id = i + 1;
                var address = rule.GetAddress(groupName, assetPath);
                var item = new AssetBundleDirectoryDetailTreeViewItem(id, $"[{address}] {assetPath}");
                _items.Add(item);
            }

            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            var height = rect.height;
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