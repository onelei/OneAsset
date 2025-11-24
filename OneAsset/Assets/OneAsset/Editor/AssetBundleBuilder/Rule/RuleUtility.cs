using System;
using System.Collections.Generic;
using OneAsset.Runtime.Core;
using OneAsset.Runtime.Rule;
using UnityEditor;

namespace OneAsset.Editor.AssetBundleBuilder.Rule
{
    public static class RuleUtility
    {
        public static string[] EncryptRules { get; private set; }
        private static readonly Dictionary<string, Type> EncryptRulesTypes = new Dictionary<string, Type>();

        static RuleUtility()
        {
            //EncryptRules
            var stringList = ListPool<string>.Get();
            var ruleTypes = TypeCache.GetTypesDerivedFrom<IEncryptRule>();
            EncryptRulesTypes.Clear();
            foreach (var type in ruleTypes)
            {
                stringList.Add(type.Name);
                EncryptRulesTypes.Add(type.Name, type);
            }

            EncryptRules = stringList.ToArray();
        }

        public static int GetAddressRuleIndex(string ruleName) => GetRuleIndex(EncryptRules, ruleName);

        private static int GetRuleIndex(string[] rules, string ruleName)
        {
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i] == ruleName)
                    return i;
            }

            return 0;
        }

        public static Type GetEncryptRuleType(string typeName) => GetRuleType(EncryptRulesTypes, typeName);

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