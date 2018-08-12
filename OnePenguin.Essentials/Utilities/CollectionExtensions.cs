using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace OnePenguin.Essentials.Utilities
{
    public static class CollectionExtensions
    {
        private static List<Type> simpleTypes = new List<Type> { typeof(int), typeof(long), typeof(byte), typeof(double), typeof(float), typeof(bool), typeof(DateTime) };

        public static IDictionary CloneDic(this IDictionary dic)
        {
            var result = Activator.CreateInstance(dic.GetType()) as IDictionary;
            foreach (var item in dic.Keys)
            {
                var key = GetCloneableObject(item);
                var value = dic[item] == null ? null : GetCloneableObject(dic[item]);
                result.Add(key, value);
            }

            return result;
        }

        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(this Dictionary<TKey, TValue> dic)
        {
            return dic.CloneDic() as Dictionary<TKey, TValue>;
        }

        public static IList CloneList(this IList list)
        {
            var result = Activator.CreateInstance(list.GetType()) as IList;

            foreach (var item in list)
            {
                result.Add(GetCloneableObject(item));
            }

            return result;
        }

        public static List<T> Clone<T>(this List<T> list)
        {
            return list.CloneList() as List<T>;
        }

        public static object CloneSet(IEnumerable hset)
        {
            var result = Activator.CreateInstance(hset.GetType()) as IEnumerable;
            var add = result.GetType().GetMethod("Add");
            foreach (var i in hset)
            {
                var obj = GetCloneableObject(i);
                add.Invoke(result, new object[] { obj });
            }

            return result;
        }

        private static object GetCloneableObject(object o)
        {
            if (simpleTypes.Contains(o.GetType()))
            {
                return o;
            }
            else if (o is string)
            {
                return string.Copy(o as string);
            }
            else if (o is ICloneable)
            {
                return (o as ICloneable).Clone();
            }
            else if (o is IDictionary)
            {
                return (o as IDictionary).CloneDic();
            }
            else if (o is IList)
            {
                return (o as IList).CloneList();
            }
            else if (o.GetType().GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return CloneSet(o as IEnumerable);
            }
            else
            {
                throw new InvalidOperationException("Can't clone " + o.GetType());
            }
        }

        public static bool SequenceEqual(this IEnumerable a, IEnumerable b)
        {
            var ea = a.GetEnumerator();
            var eb = b.GetEnumerator();
            while (ea.MoveNext())
            {
                if (!eb.MoveNext()) return false;

                if (!ea.Current.Equals(eb.Current)) return false;
            }

            if (eb.MoveNext()) return false;

            return true;
        }

        public static void CreateOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue obj)
        {
            if (dic.ContainsKey(key))
                dic[key] = obj;
            else
                dic.Add(key, obj);
        }

        public static void CreateOrAddToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dic, TKey key, TValue obj)
        {
            if (dic.ContainsKey(key))
                dic[key].Add(obj);
            else
                dic.Add(key, new List<TValue> { obj });
        }

        public static void CreateOrAddToList<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dic, TKey key, TValue obj)
        {
            if (dic.ContainsKey(key))
                dic[key].Add(obj);
            else
                dic.Add(key, new HashSet<TValue> { obj });
        }

        public static void CreateOrAddRangeToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dic, TKey key, List<TValue> obj)
        {
            if (dic.ContainsKey(key))
                dic[key].AddRange(obj);
            else
                dic.Add(key, new List<TValue>(obj));
        }

        public static void CreateOrAddRangeToList<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dic, TKey key, HashSet<TValue> obj)
        {
            if (dic.ContainsKey(key))
                dic[key].AddRange(obj);
            else
                dic.Add(key, new HashSet<TValue>(obj));
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

        public static HashSet<TValue> UnionAllValues<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> dic)
        {
            var result = new HashSet<TValue>();
            foreach (var v in dic.Values) result.AddRange(v);
            return result;
        }

        public static void AddRange<T>(this HashSet<T> hset, IEnumerable<T> v)
        {
            foreach (var i in v) hset.Add(i);
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (var i in list) action(i);
        }

        public static bool EqualsTo<TKey, TValue>(this Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
        {
            if (dict1 == dict2) return true;
            if ((dict1 == null) || (dict2 == null)) return false;
            if (dict1.Count != dict2.Count) return false;

            foreach (var kvp in dict1)
            {
                TValue value2;
                if (!dict2.TryGetValue(kvp.Key, out value2)) return false;

                if (value2.GetType().GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    if (!kvp.Value.Equals(value2)) return false;
                }
                if (value2 is IEnumerable)
                {
                    if (!(value2 as IEnumerable).SequenceEqual(kvp.Value as IEnumerable)) return false;
                }
                else if (!kvp.Value.Equals(value2))
                {
                    return false;
                }
            }
            return true;
        }
    }

}