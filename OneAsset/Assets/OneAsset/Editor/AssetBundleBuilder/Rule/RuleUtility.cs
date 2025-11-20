using System;
using System.Collections.Generic;
using OneAsset.Runtime.Core;
using OneAsset.Runtime.Rule;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Rule
{
    public static class RuleUtility
    {
        public static string[] EntryptRules { get; private set; }
        private static readonly Dictionary<string, Type> EntryptRulesTypes = new Dictionary<string, Type>();

        static RuleUtility()
        {
            //EntryptRules
            var stringList = ListPool<string>.Get();
            var ruleTypes = TypeCache.GetTypesDerivedFrom<IEntryptRule>();
            EntryptRulesTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                EntryptRulesTypes.Add(type.Name, type);
            }

            EntryptRules = stringList.ToArray();
        }

        public static int GetAddressRuleIndex(string ruleName) => GetRuleIndex(EntryptRules, ruleName);

        private static int GetRuleIndex(string[] rules, string ruleName)
        {
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i] == ruleName)
                    return i;
            }

            return 0;
        }

        public static Type GetEntryptRuleType(string typeName) => GetRuleType(EntryptRulesTypes, typeName);

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