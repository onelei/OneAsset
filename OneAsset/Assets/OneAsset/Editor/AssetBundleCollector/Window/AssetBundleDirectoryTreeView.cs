using System.Collections.Generic;
using OneAsset.Editor.AssetBundleCollector.Data;
using OneAsset.Editor.AssetBundleCollector.Rule;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace OneAsset.Editor.AssetBundleCollector.Window
{
    internal sealed class AssetBundleDirectoryTreeViewItem : TreeViewItem
    {
        public readonly int Index;
        private string _groupName;
        public readonly AssetBundleDirectory Data;
        public int AddressRuleTypeIndex;
        public int CollectorRuleTypeIndex;
        public int PackRuleTypeIndex;
        public int FilterRuleTypeIndex;
        public DefaultAsset PathAsset;
        public bool IsFoldout = false;
        public List<string> Assets;

        public AssetBundleDirectoryTreeViewItem(int index, string groupName, AssetBundleDirectory directory)
        {
            this.Index = index;
            _groupName = groupName;
            Data = directory;
            AddressRuleTypeIndex = RuleUtility.GetAddressRuleIndex(Data.addressRuleType);
            CollectorRuleTypeIndex = RuleUtility.GetCollectorRuleIndex(Data.collectorType);
            PackRuleTypeIndex = RuleUtility.GetPackRuleIndex(Data.packRuleType);
            FilterRuleTypeIndex = RuleUtility.GetFilterRuleIndex(Data.filterRuleType);
            id = index;
            displayName = directory.path;
            depth = 0;
            if (!string.IsNullOrEmpty(Data.path))
            {
                PathAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(Data.path);
            }

            Refresh();
        }

        public void Refresh()
        {
            var mainAssets = Data.GetMainAssets();
            var rule = Data.GetAddressRule();
            Assets = new List<string>(mainAssets.Count);
            foreach (var assetPath in mainAssets)
            {
                var address = rule.GetAddress(_groupName, assetPath);
                Assets.Add($"[{address}] {assetPath}");
            }
        }
    }

    public class AssetBundleDirectoryTreeView : TreeView
    {
        private readonly SearchField _searchField;
        private static readonly List<int> EmptyList = new List<int>();
        private const float Offset = 2;
        private readonly List<TreeViewItem> _items = new List<TreeViewItem>();
        private AssetBundleCollectorGroup _group;

        public AssetBundleDirectoryTreeView() : base(new TreeViewState())
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _searchField = new SearchField();
            Reload();
        }

        public void SetData(AssetBundleCollectorGroup data)
        {
            _group = data;
            _items.Clear();
            if (_group != null)
            {
                for (var i = 0; i < _group.directories.Count; i++)
                {
                    var package = _group.directories[i];
                    var id = i + 1;
                    var item = new AssetBundleDirectoryTreeViewItem(id, _group.groupName, package);
                    _items.Add(item);
                }
            }

            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            if (_group == null)
                return;
            var height = rect.height;
            var width = rect.width;
            var singleLineHeight = EditorGUIUtility.singleLineHeight;
            rect.height = singleLineHeight;
            //groupActive
            _group.groupActive = (EGroupActive) EditorGUI.EnumPopup(rect, "Group Active", _group.groupActive);
            rect.y += rect.height + Offset;
            //groupName
            _group.groupName = EditorGUI.TextField(rect, "Group Name", _group.groupName);
            rect.y += rect.height + Offset;
            //groupDesc
            _group.groupDesc = EditorGUI.TextField(rect, "Group Desc", _group.groupDesc);
            rect.y += rect.height + Offset;
            //tags
            _group.tags = EditorGUI.TextField(rect, "Group Tags", _group.tags);
            //SearchField
            rect.y += rect.height + Offset;
            searchString = _searchField.OnGUI(rect, searchString);
            //TreeView
            rect.y += rect.height + Offset;
            rect.height = height - (singleLineHeight + Offset) * 6;
            base.OnGUI(rect);
            //Button
            rect.y += rect.height + Offset;
            rect.height = singleLineHeight;
            if (GUI.Button(rect, "+"))
            {
                _group.directories.Add(new AssetBundleDirectory() {path = string.Empty});
                SetData(_group);
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
            SetSelection(EmptyList);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AssetBundleDirectoryTreeViewItem) args.item;
            var rect = args.rowRect;
            var width = rect.width;
            var singleLineHeight = EditorGUIUtility.singleLineHeight;
            var y = rect.y;
            rect.height = singleLineHeight;
            rect.width = 20;
            width -= rect.width;
            if (GUI.Button(rect, "-"))
            {
                if (EditorUtility.DisplayDialog("Remove Confirm", "Are you sure?", "OK", "Cancel"))
                {
                    _group.directories.Remove(item.Data);
                    SetData(_group);
                }
            }

            rect.x += rect.width;
            width -= rect.width;
            if (GUI.Button(rect, "+"))
            {
                _group.directories.Insert(item.Index, new AssetBundleDirectory() {path = string.Empty});
                SetData(_group);
            }

            //PathAsset
            rect.x += rect.width;
            rect.width = 150;
            EditorGUI.BeginChangeCheck();
            item.PathAsset = (DefaultAsset) EditorGUI.ObjectField(rect, item.PathAsset, typeof(DefaultAsset), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (item.PathAsset != null)
                {
                    item.Data.path = AssetDatabase.GetAssetPath(item.PathAsset);
                    item.Refresh();
                }
            }

            //Path
            var x = rect.x;
            rect.x += rect.width;
            rect.width = width - rect.width;
            EditorGUI.LabelField(rect, item.Data.path);
            //collectorType
            rect.x = x;
            var height = singleLineHeight + Offset;
            rect.y += height;
            rect.width = 150;
            EditorGUI.BeginChangeCheck();
            item.CollectorRuleTypeIndex =
                EditorGUI.Popup(rect, item.CollectorRuleTypeIndex, RuleUtility.CollectorRules);
            if (EditorGUI.EndChangeCheck())
            {
                item.Data.collectorType = RuleUtility.CollectorRules[item.CollectorRuleTypeIndex];
            }

            //addressRuleType
            rect.x += rect.width;
            EditorGUI.BeginChangeCheck();
            item.AddressRuleTypeIndex =
                EditorGUI.Popup(rect, item.AddressRuleTypeIndex, RuleUtility.AddressRules);
            if (EditorGUI.EndChangeCheck())
            {
                item.Data.addressRuleType = RuleUtility.AddressRules[item.AddressRuleTypeIndex];
            }

            //packRuleType
            rect.x += rect.width;
            EditorGUI.BeginChangeCheck();
            item.PackRuleTypeIndex =
                EditorGUI.Popup(rect, item.PackRuleTypeIndex, RuleUtility.PackRules);
            if (EditorGUI.EndChangeCheck())
            {
                item.Data.packRuleType = RuleUtility.PackRules[item.PackRuleTypeIndex];
            }

            //filterRuleType
            rect.x += rect.width;
            EditorGUI.BeginChangeCheck();
            item.FilterRuleTypeIndex =
                EditorGUI.Popup(rect, item.FilterRuleTypeIndex, RuleUtility.FilterRules);
            if (EditorGUI.EndChangeCheck())
            {
                item.Data.filterRuleType = RuleUtility.FilterRules[item.FilterRuleTypeIndex];
            }

            //args
            rect.x += rect.width + 10;
            rect.width = 60;
            EditorGUI.LabelField(rect, "Args");
            rect.x += rect.width;
            rect.width = 150;
            item.Data.args = EditorGUI.TextField(rect, item.Data.args);
            //Tags
            rect.x += rect.width + 10;
            rect.width = 60;
            EditorGUI.LabelField(rect, "Tags");
            rect.x += rect.width;
            rect.width = 150;
            item.Data.tags = EditorGUI.TextField(rect, item.Data.tags);
            //Detail
            rect.x = x;
            rect.y += height;
            rect.width = 20;
            if (GUI.Button(rect, "?"))
            {
                AssetBundleDirectoryDetailWindow.Open(_group.groupName, item.Data);
            }

            //Assets
            rect.x += rect.width;
            rect.width = 150;
            EditorGUI.BeginChangeCheck();
            item.IsFoldout = EditorGUI.Foldout(rect, item.IsFoldout, "Assets");
            if (EditorGUI.EndChangeCheck())
            {
                Reload();
            }

            if (item.IsFoldout)
            {
                rect.width = width;
                foreach (var asset in item.Assets)
                {
                    rect.x = x + 35;
                    rect.y += height;
                    EditorGUI.LabelField(rect, asset);
                }
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            var treeViewItem = (AssetBundleDirectoryTreeViewItem) item;
            var singleLineHeight = EditorGUIUtility.singleLineHeight;
            var heightOffset = singleLineHeight + Offset;
            var height = heightOffset * 4;
            if (treeViewItem.IsFoldout)
            {
                height += treeViewItem.Assets.Count * heightOffset;
            }

            return height;
        }
    }
}