using System;
using System.Collections.Generic;

namespace OnePenguin.Essentials.Utilities
{
    public static class CollectionExtensions
    {
        public static void CreateOrAddToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dic, TKey key, TValue obj)
        {
            if (dic.ContainsKey(key))
                dic[key].Add(obj);
            else
                dic.Add(key, new List<TValue> { obj });
        }

        public static void CreateOrAddRangeToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dic, TKey key, List<TValue> obj)
        {
            if (dic.ContainsKey(key))
                dic[key].AddRange(obj);
            else
                dic.Add(key, new List<TValue>(obj));
        }

        public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dic, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var kvp in dic) action(kvp);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dic)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var kvp in dic) result.Add(kvp.Key, kvp.Value);
            return result;
        }

        public static List<TValue> UnionAllValues<TKey, TValue>(this IDictionary<TKey, List<TValue>> dic)
        {
            var result = new List<TValue>();
            foreach (var v in dic.Values) result.AddRange(v);
            return result;
        }
    }
}