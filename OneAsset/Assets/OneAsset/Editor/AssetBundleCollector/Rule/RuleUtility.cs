using System;
using System.Collections.Generic;
using System.IO;
using OneAsset.Runtime.Core;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleCollector.Rule
{
    public static class RuleUtility
    {
        public static string[] AddressRules { get; private set; }
        private static readonly Dictionary<string, Type> AddressTypes = new Dictionary<string, Type>();
        public static string[] CollectorRules { get; private set; }
        private static readonly Dictionary<string, Type> CollectorTypes = new Dictionary<string, Type>();
        public static string[] PackRules { get; private set; }
        private static readonly Dictionary<string, Type> PackTypes = new Dictionary<string, Type>();
        public static string[] FilterRules { get; private set; }
        private static readonly Dictionary<string, Type> FilterTypes = new Dictionary<string, Type>();

        static RuleUtility()
        {
            //AddressRules
            var stringList = ListPool<string>.Get();
            var ruleTypes = TypeCache.GetTypesDerivedFrom<IAddressRule>();
            AddressTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                AddressTypes.Add(type.Name, type);
            }

            AddressRules = stringList.ToArray();
            //CollectRules
            stringList.Clear();
            ruleTypes = TypeCache.GetTypesDerivedFrom<ICollectorRule>();
            CollectorTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                CollectorTypes.Add(type.Name, type);
            }

            CollectorRules = stringList.ToArray();
            //PackRules
            stringList.Clear();
            ruleTypes = TypeCache.GetTypesDerivedFrom<IPackRule>();
            PackTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                PackTypes.Add(type.Name, type);
            }

            PackRules = stringList.ToArray();
            //FilterRules
            stringList.Clear();
            ruleTypes = TypeCache.GetTypesDerivedFrom<IFilterRule>();
            FilterTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                FilterTypes.Add(type.Name, type);
            }

            FilterRules = stringList.ToArray();
            ListPool<string>.Release(stringList);
        }

        public static int GetAddressRuleIndex(string ruleName) => GetRuleIndex(AddressRules, ruleName);
        public static int GetCollectorRuleIndex(string ruleName) => GetRuleIndex(CollectorRules, ruleName);
        public static int GetPackRuleIndex(string ruleName) => GetRuleIndex(PackRules, ruleName);
        public static int GetFilterRuleIndex(string ruleName) => GetRuleIndex(FilterRules, ruleName);

        private static int GetRuleIndex(string[] rules, string ruleName)
        {
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i] == ruleName)
                    return i;
            }

            return 0;
        }

        public static Type GetAddressRuleType(string typeName) => GetRuleType(AddressTypes, typeName);
        public static Type GetCollectorRuleType(string typeName) => GetRuleType(CollectorTypes, typeName);
        public static Type GetPackRuleType(string typeName) => GetRuleType(PackTypes, typeName);
        public static Type GetFilterRuleType(string typeName) => GetRuleType(FilterTypes, typeName);

        private static Type GetRuleType(Dictionary<string, Type> types, string typeName)
        {
            if (types.TryGetValue(typeName, out var type))
            {
                return type;
            }

            return null;
        }
    }
}